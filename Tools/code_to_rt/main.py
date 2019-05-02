#!/usr/bin/env python3

# Code by Leon H.
# github.com/S1r0hub

import os
import argparse
import logging
import json

# requires pygments to be installed
from pygments import highlight
from pygments.lexers import get_lexer_for_filename
from pygments.formatters import HtmlFormatter
from pygments.util import ClassNotFound

from htmlParser import HtmlParser

LOGGER = None


def main():

    # create argument parser
    parser = argparse.ArgumentParser(
        description='Convert ConfigCrusher program measurement results.',
        formatter_class=argparse.ArgumentDefaultsHelpFormatter
    )

    # add arguments to parser and parse
    prepareParser(parser)
    args = parser.parse_args()


    # set up logger
    global LOGGER
    LOGGER = logging.getLogger('crusherToJSONLogger')
    LOGGER.setLevel(logging.DEBUG)

    # check if debug should be enabled
    logLevel = logging.INFO
    if args.verbose: logLevel = logging.DEBUG

    # channel to stream log events to console
    ch = logging.StreamHandler()
    ch.setLevel(logLevel)
    formatter = logging.Formatter('[%(levelname)s] (%(asctime)s): %(message)s')
    ch.setFormatter(formatter)
    LOGGER.addHandler(ch)

    # log to file if enabled
    logPath = args.logfile
    if len(logPath) > 0:
        if not logPath.endswith(".log"):
            logPath += ".log"
        fileHandler = logging.FileHandler(logPath)
        fileHandler.setFormatter(formatter)
        LOGGER.addHandler(fileHandler)
    LOGGER.info('Logger ready.')


    # validate output folder
    outFolder = args.outpath
    if not (outFolder.endswith("/") or outFolder.endswith("\\")):
        outFolder += "/"

    if not os.path.exists(outFolder):
        LOGGER.warning('The output folder does not exist! Creating it...')

        try: os.makedirs(outFolder)
        except Exception as ex:
            LOGGER.exception('Failed to create output folder!')
            return

        outFolder = os.path.normcase(outFolder)
        LOGGER.info('Output folder created: {}'.format(outFolder))

    else:

        # check that path leads to a folder
        if not os.path.isdir(outFolder):
            LOGGER.error('The output folder path does not lead to a folder!')
            return


    # validate color schema file
    schemaPath = args.colorschema
    if not os.path.isfile(schemaPath):
        LOGGER.error('The given schema path is no valid file: {}'.format(schemaPath))
        return


    # check if recursive export is desired
    recursive = True if args.recursive else False

    # check if user wants to overwrite existing files
    overwrite = True if args.overwrite else False

    # export the highlighted HTML code as well if desired
    exportHTML = True if args.exporthtml else False
    if exportHTML: LOGGER.info('Additional HTML export enabled.')


    # try to read JSON color schema
    jsonSchema = None
    with open(schemaPath, "r") as file:
        try:
            jsonSchema = json.loads(file.read())
        except Exception as ex:
            LOGGER.error(ex)
    if jsonSchema is None: return


    # check if path exists
    filePath = args.path
    if not os.path.exists(filePath):
        LOGGER.error('Failed to convert! Given path does not exist: {}'.format(filePath))
        return None

    # check if path leads to file or folder
    if os.path.isfile(filePath):

        # parses html code to unity rt format
        parser = HtmlParser(colorSchema=jsonSchema)

        # convert a file and export the result
        LOGGER.info('Converting the file...')
        resultPath = convertFile(
            htmlParser=parser,
            filePath=filePath,
            outputFolder=outFolder,
            exportHTML=exportHTML,
            overwrite=overwrite
        )

    elif os.path.isdir(filePath):

        # convert all files of the folder
        LOGGER.info('Converting the files{}...'.format(' recursively' if recursive else ''))
        resultPath = convertFiles(
            folderPath=filePath,
            outputFolder=outFolder,
            jsonSchema=jsonSchema,
            exportHTML=exportHTML,
            overwrite=overwrite,
            recursive=recursive
        )

    # print result path
    if not resultPath is None:
        LOGGER.info('Result path: ' + os.path.abspath(resultPath))


def convertFiles(folderPath, outputFolder, jsonSchema, exportHTML=False, overwrite=False, recursive=False):
    '''
    Converts all files source code to a syntax highlighted rich text format.
    This method does not check if the given path is valid!
    Returns None on errors, the path to the exported files otherwise.
    '''

    firstOutPath = None
    pathLength = len(folderPath)
    if folderPath.endswith('/') or folderPath.endswith('\\'): folderPath = folderPath[:-1]
    srcDirName = os.path.normcase(os.path.basename(folderPath))
    outputFolder = os.path.normcase(os.path.normpath(outputFolder))

    for curDir, subDirs, files in os.walk(folderPath, topdown=True):
        curDir_relative = os.path.normpath(os.path.join(srcDirName, curDir[pathLength:]))
        LOGGER.info('Entering directory: {}'.format(curDir_relative))

        # create export path
        #LOGGER.debug('Joining paths "{}" and "{}"'.format(outputFolder, curDir_relative))
        curOutFolder = os.path.normcase(os.path.join(outputFolder, curDir_relative))
        LOGGER.debug('Current output folder: {}'.format(curOutFolder))
        if os.path.exists(curOutFolder):
            if os.path.isfile(curOutFolder):
                LOGGER.error('Failed to export to: {} (is a file instead of a folder)'.format(os.path.abspath(curOutFolder)))
                return None
        else:
            # create the output folder
            LOGGER.info('Creating folder: {}'.format(curOutFolder))
            try: os.mkdir(curOutFolder)
            except Exception as ex:
                LOGGER.exception('Failed to create an output folder: {}'.format(curOutFolder))
                return None

        if firstOutPath is None: firstOutPath = curOutFolder

        # convert and export all the files of this folder
        for file in files:
            
            # parses html code to unity rt format
            parser = HtmlParser(colorSchema=jsonSchema)

            LOGGER.info('Converting file: {}'.format(file))
            path = convertFile(
                htmlParser=parser,
                filePath=os.path.join(curDir, file),
                outputFolder=curOutFolder,
                exportHTML=exportHTML,
                overwrite=overwrite
            )

            if not path is None: LOGGER.info('File exported: {}'.format(path))

        # do not take sub-folders into account if recursion is disabled
        if not recursive: break

    return firstOutPath


def convertFile(htmlParser, filePath, outputFolder, exportHTML=False, overwrite=False):
    '''
    Converts source code to a syntax highlighted rich text format.
    This method does not check if the given path is valid!
    Returns None on errors, the exported file path otherwise.
    '''

    # highlight source code and get HTML result
    htmlCode = None
    with open(filePath, "r") as codeFile:
        LOGGER.debug('Highlighting file: {}'.format(codeFile.name))
        htmlCode = highlightCode(codeFile, codeFile.read())
        LOGGER.debug('Finished highlighting!')

    if htmlCode is None: return None

    # export HTML result to file
    if exportHTML:
        LOGGER.debug('Exporting HTML code highlighting to file...')
        htmlOutputPath = os.path.join(outputFolder, os.path.basename(filePath) + '.html')
        success = writeToFile(filePath=htmlOutputPath, data=htmlCode, overwrite=overwrite)
        if success: LOGGER.info('Exported HTML file to: ' + os.path.abspath(htmlOutputPath))

    # convert HTML to the Rich Text format of Unity3D
    LOGGER.debug('Converting HTML to Unity Rich Text format...')
    htmlParser.feed(htmlCode)
    richText = htmlParser.getRichText()
    LOGGER.debug('Finished converting.')

    # export Rich Text result to file
    LOGGER.debug('Exporting result to file...')
    outFilePath = os.path.join(outputFolder, os.path.basename(filePath) + '.rt')
    success = writeToFile(filePath=outFilePath, data=richText, overwrite=overwrite)

    # return file path on success
    if success:
        LOGGER.info('Exported RT file to: ' + os.path.abspath(outFilePath))
        return outFilePath

    return None


def writeToFile(filePath, data, overwrite=False):
    '''
    Write data to a file and if enabled, overwrite existing file.
    Returns True if data was written to the file, False otherwise.
    '''
    
    # check if file already exists
    if os.path.isfile(filePath):
        if overwrite:
            LOGGER.warning('File already exists. Overwriting it. ({})'.format(filePath))
        else:
            LOGGER.error('File already exists! ({})'.format(filePath))
            return False

    # try to perform export to file
    try:
        with open(filePath, "w") as outFile:
            outFile.write(data)
    except Exception as ex:
        LOGGER.error('Failed to write data to file! ({})'.format(filePath))
        LOGGER.error(str(ex))
        return False

    return True


def highlightCode(file, code):
    '''
    Highlight the passed code and returns formatted HTML code.
    - code: string
    - file: file
    Returns the highlighted code in HTML format or None on errors.
    '''

    lexer = None
    formatter = None

    try:
        # http://pygments.org/docs/api/#pygments.lexers.get_lexer_for_filename
        lexer = get_lexer_for_filename(file.name)
        LOGGER.info('Found fitting lexer: {}, {}'.format(lexer.name, lexer.mimetypes))
    except ClassNotFound as cnf:
        LOGGER.error('Could not find a fitting lexer for file: {}'.format(file.name))
        #LOGGER.error(str(cnf))
        return None

    # http://pygments.org/docs/api/#pygments.formatters.get_formatter_for_filename
    # get_formatter_for_filename(file.name)
    formatter = HtmlFormatter()

    return highlight(code, lexer, formatter)


def prepareParser(parser):
    '''
    Prepares the argument parser by adding required arguments to it.
    '''

    parser.add_argument('-p', '-path', '--path', required=True, type=str,
        help='Path to the source code')

    parser.add_argument('-r', '-recursive', '--recursive', required=False, action='store_true',
        help='Add this flag to convert recursively (include sub-folders and files) if a folder is given as the path')

    parser.add_argument('-o', '-outpath', '--outpath', required=True, type=str,
        help='Path of the exported files')

    parser.add_argument('-c', '-cs', '-colorschema', '--colorschema', required=True, type=str,
        help='Path to a JSON file that contains a JSON Object with key = class and value = color value')

    parser.add_argument('-lf', '-logfile', '--logfile', required=False, type=str, default="logging",
        help='Path and name of the log file. Set empty to disable logging to a file.')

    parser.add_argument('-ehtml', '-exporthtml', '--exporthtml', help='Enable additional HTML export', action='store_true')

    parser.add_argument('-ow', '-overwrite', '--overwrite', required=False, action='store_true',
        help='Add this flag to overwrite output files that already exist')

    parser.add_argument('-v', '-verbose', '--verbose', required=False, action='store_true',
        help='Add this flag for verbose output (debug logging enabled)')


if __name__ == '__main__':
    main()
