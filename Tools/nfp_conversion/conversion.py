#!/usr/bin/env python3

# Code by Leon H.
# github.com/S1r0hub
#
# This tool converts the catena measurement data
# in the fomat supported by the VRVis application.
#
# Currently, there is only Java tested and supported
# as the programming language of the source code.
# Methods are searched using curly brackets.
#
# Requires the library "javalang" to parse the Java code:
# - https://github.com/c2nes/javalang
#
# Notes:
# - The method name "<init>" marks all constructors bc. example data did not yield signature
# - The case that a method is overwritten is not handled yet bc. there was no such example data yet

import os
import json
import uuid
import parser_and_logger
from collections import OrderedDict

# additional library
import javalang

LOGGER = None


### SETTINGS ###

# ignored if less than 0
DECIMALS_AFTER_COMMA = 4
EXPORT_NAME = "converted.json" # filename (set per argument)
EXPORT_INDENTED = False # results in ugly but smaller files (set per argument)
PROPERTY_TYPE = "nfp"


def main():

    # prepare argument parsing
    parser = parser_and_logger.prepareParser(description='Tool to convert Catena performance measurements.')
    args = parser.parse_args()

    # prepare logging
    global LOGGER
    LOGGER = parser_and_logger.prepareLogger(name='conversionLogger', logPath=args.logfile, verboseLogging=args.verbose)

    # get indentation setting
    global EXPORT_INDENTED
    EXPORT_INDENTED = False if args.no_indentation else True

    # get export name setting
    global EXPORT_NAME
    EXPORT_NAME = args.outname

    # validate the output folder
    outpath = validateOutputPath(args.outpath)

    # start conversion
    convert(
        inFilePath=args.measurements_path,
        programPath=args.program_path,
        srcCodeExtension=args.source_code_extension,
        outputPath=outpath,
        propertyName=args.property_name,
        overwrite=args.overwrite,
        debug=args.verbose
    )


def convert(inFilePath, programPath, srcCodeExtension, outputPath, propertyName, overwrite=False, debug=False):
    '''
    Converts the catena measurements in the format
    that can be used by the VRVis application.
    JSON files following the naming convention "regions_<name>.json"
    will be exported if a region for a file could be found.

    Parameters:
    - inFilePath: input file with measurements
    - programPath: path to folder with program files the measurements refer to (e.g. "src")
    - outputPath: folder to write output files with regions containing the measurements
    - srcCodeExtension: extension of source code files (e.g. ".java")
    - debug: log debug information (required the according logging level to be set as well)

    The input format looks as follows:
    "main.java.MyClassName:MyFunction [...]"

    So what we do to extract the required information:
    - 1. Split at "[" -> result of two sections: [0] = path & method, [1] = values
    - 2. Split the result[0]  at ":" -> result of two sections: [0] = path, [1] = method
    - 3. Group all the information by file path
         - each file with methods and each method with values
    - 4. Try to find the according files by name (e.g. Catena.java)
    - 5. Try to find the position of the according method in the file (line position)
    - 6. Find end of the method (line position)
    - 7. Convert this information to the region format and store in the file.
    '''

    lineNo = 0
    groupedData = {} # format: { dotFilePath: methodName: [...] }
    firstArrayLength = -1
    methodsTotal = 0

    # load data from file
    with open(inFilePath, "r") as inFile:
        for line in inFile:

            lineNo += 1

            # skip empty lines
            if len(line) == 0:
                LOGGER.warning('Empty line: {}'.format(lineNo))
                continue

            # step 1., 2., 3.
            dataDict = prepareData(line, lineNo)
            if dataDict is None: continue

            # set first array length to compare others with
            valuesArray = dataDict['values']
            curArrayLength = len(valuesArray)
            if firstArrayLength < 0:
                firstArrayLength = curArrayLength

            # validate this array length
            elif curArrayLength != firstArrayLength:
                LOGGER.warning('Line {}: Array length ({})' \
                    'does not match first one ({})'.format(lineNo, curArrayLength, firstArrayLength))

            # add to grouped data
            fPath = dataDict['file']
            fMethod = dataDict['method']
            if not fPath in groupedData:
                groupedData[fPath] = { fMethod: valuesArray }
                methodsTotal += 1
                continue

            # check if this method already has values added to it and if so, skip it
            if fMethod in groupedData[fPath]:
                LOGGER.error('Line {}: Method "{}" has already values assigned! Skipping this line.')
                continue

            # everything okay - just add the method with its values
            groupedData[fPath][fMethod] = valuesArray
            methodsTotal += 1

    LOGGER.info('Finished loading data from file (files: {}, methods: {})'.format(len(groupedData), methodsTotal))

    if debug:
        fileNum = 0
        for path in groupedData:
            fileNum += 1
            LOGGER.debug('File {}: {}'.format(fileNum, path))


    # create list of files with information about usage
    # ("could this information be converted to regions?")
    filesUsed = {file: False for file in groupedData}

    # try to find matching files and methods
    if programPath.endswith('/') or programPath.endswith('\\'): programPath = programPath[:-1]
    outputFolder = os.path.normcase(os.path.normpath(outputPath))
    srcDirName = os.path.normcase(os.path.basename(programPath)) + '/'
    pathLength = len(programPath)

    # dict of {relative_file_path: method_name: {from, to}}
    fileMethodPositions = {}
    groupedDataLink = {} # relative_file_path -> file path of groupedData

    for curDir, subDirs, files in os.walk(programPath, topdown=True):

        # relative path including the "src" folder
        pathPart2 = curDir[pathLength:]
        if pathPart2.startswith('/') or pathPart2.startswith('\\'): pathPart2 = pathPart2[1:]
        curDir_relative = os.path.normpath(os.path.join(srcDirName, pathPart2))

        # relative to the "src" folder (without "src" folder in path)
        curDir_relative_src = os.path.normpath(curDir[pathLength:]) # relative to source directory
        LOGGER.info('Processing directory (files: {}): {}'.format(len(files), curDir_relative_src))

        # replace slash by dot to get formatted file path
        pathFormatted = curDir_relative_src.replace('\\', '.').replace('/', '.')
        if pathFormatted.startswith('.'): pathFormatted = pathFormatted[1:]
        if pathFormatted.endswith('.'): pathFormatted = pathFormatted[:-1]

        # check files
        for file in files:

            # skip files that do not match the extension
            if not file.lower().endswith(srcCodeExtension): continue
            fileNoExt = file[:-len(srcCodeExtension)]

            # search for matching file in loaded data
            filePathFormatted = pathFormatted + '.' + fileNoExt
            if not filePathFormatted in groupedData:
                if debug: LOGGER.debug('File NOT found in grouped data: {}'.format(filePathFormatted))
                continue
            if debug: LOGGER.debug('File FOUND in grouped data: {}'.format(filePathFormatted))

            # note this file as used (for conversion summary)
            filesUsed[filePathFormatted] = True

            # search for methods in file
            filePath = os.path.normpath(curDir + '/' + file)
            methodPositions = findMethodPositionsJava(
                methodNames=[method for method in groupedData[filePathFormatted]],
                filePath=filePath,
                filenameNoExt=fileNoExt,
                debug=debug
            )

            if methodPositions is None: continue

            # use relative path with source directory
            relFilePath_region = curDir_relative.replace('\\', '/') + '/' + file
            if not relFilePath_region in fileMethodPositions:
                fileMethodPositions[relFilePath_region] = {}
                groupedDataLink[relFilePath_region] = filePathFormatted

            for methodName in methodPositions:
                fileMethodPositions[relFilePath_region][methodName] = methodPositions[methodName]
                #groupedDataLink[relFilePath_region][filePathFormatted].append(methodName)


    LOGGER.info('Finished processing!')


    if debug:
        for entry in fileMethodPositions:
            LOGGER.debug('---> {}: {}'.format(entry, fileMethodPositions[entry]))

        LOGGER.debug('Printing grouped data links:')
        for e in groupedDataLink:
            LOGGER.info('{} ==> {}'.format(e, groupedDataLink[e]))


    # check if all files have been used
    allFilesUsed = True
    for fName in filesUsed:
        if not filesUsed[fName]:
            LOGGER.warning('File not used: {}'.format(fName))
            allFilesUsed

    if allFilesUsed:
        LOGGER.info('All files given by the data have been used.')


    # prepare output file
    outputFilePath = os.path.normpath(os.path.normcase(outputFolder + '/' + EXPORT_NAME))
    LOGGER.info('Exporting to: {}'.format(os.path.abspath(outputFilePath)))

    if os.path.exists(outputFilePath):
        if not overwrite:
            LOGGER.error('Failed to export results! File already exists. Consider using the overwrite flag.')
            return
        else: LOGGER.warning('Overwriting existing export file!')


    # create the JSON file content
    jsonContent = { 'regions': [] }

    for location in fileMethodPositions:
        for method in fileMethodPositions[location]:

            fromLine = fileMethodPositions[location][method]['from']
            toLine = fromLine + 1

            if 'to' in fileMethodPositions[location][method]:
                toLine = fileMethodPositions[location][method]['to']
            else:
                LOGGER.warning('Missing "to line" value - Location: {}, Method: {}'.format(location, method))

            # generated UUID by location and method combination and property added
            regionID = str(uuid.uuid3(uuid.NAMESPACE_X500, location + ":" + method + ":" + propertyName))

            # get values array
            valuesArray = None
            if location in groupedDataLink:
                locationFormatted = groupedDataLink[location] # (with "." instead of "/")
                if method in groupedData[locationFormatted]:
                    valuesArray = groupedData[locationFormatted][method]

            if valuesArray is None:
                LOGGER.error('Failed to get values array! Skipping. - Location: {}, Method: {}'.format(location, method))
                continue

            # create the region JSON entry
            regionEntry = OrderedDict([
                ('id', regionID),
                ('location', location),
                ('nodes', str(fromLine) + "-" + str(toLine)),
                ('properties', [{
                    'type': PROPERTY_TYPE,
                    'name': propertyName,
                    'value': valuesArray
                }])
            ])

            jsonContent['regions'].append(regionEntry)


    # export regions (jsonContent) to the output file
    with open(outputFilePath, 'w') as outFile:
        indentation = 4 if EXPORT_INDENTED else None
        json.dump(jsonContent, outFile, ensure_ascii=False, indent=indentation)

    LOGGER.info('Finished export to: {}'.format(outputFilePath))


def validateOutputPath(path):
    '''
    Validates the output path by checking if it exists and is a valid folder.
    It also creates a missing directory if desired.
    Returns the path to use or None if invalid.
    '''

    # add trailing slash
    if not (path.endswith("/") or path.endswith("\\")): path += "/"
    absPath = os.path.abspath(path)

    # use if exists, create otherwise
    if not os.path.exists(path):

        LOGGER.warning('The output folder does not exist! Creating it...')

        try: os.makedirs(path)
        except Exception as ex:
            LOGGER.exception('Failed to create output folder!')
            return None

        path = os.path.normcase(path)
        LOGGER.info('Output folder created: {}'.format(absPath))

    else:

        # check that path leads to a folder
        if not os.path.isdir(path):
            LOGGER.error('The output folder path does not lead to a folder: {}'.format(absPath))
            return None

    # check if permission exists
    if not os.access(path, os.W_OK):
        LOGGER.error('No permission to write to output folder: {}'.format(absPath))
        return None

    return path


def prepareData(line, lineNo):
    '''
    Gather required information from that line as described in steps 1-3
    and return the result in form of a dictionary.
    Returns None on errors (e.g. missing required information).

    Returned dictionary keys:
    - file
    - method
    - values
    '''

    err_msg = 'Failed to parse line {}'.format(lineNo)


    # split at the first occurrence of '['
    split1 = line.split('[', 1)

    if len(split1) != 2:
        LOGGER.error(err_msg + ' - split1 wrong size!')
        return None


    # split first part at first occurrence of ':'
    split2 = split1[0].split(':', 1)

    if len(split2) != 2:
        LOGGER.error(err_msg + ' - split2 wrong size!')
        return None


    # validate file path
    filePath = split2[0].strip()
    if len(filePath) == 0:
        LOGGER.error(err_msg + ' - invalid file path: {}'.format(filePath))
        return None

    # validate method name
    methodName = split2[1].strip()
    if len(methodName) == 0:
        LOGGER.error(err_msg + ' - invalid method name: {}'.format(methodName))
        return None


    # parse values array from string
    valuesStr = split1[1].split(']', 1)[0]
    valuesArray = []
    entryNo = 0

    for numStr in valuesStr.split(','):

        entryNo += 1

        num = 0
        try: num = float(numStr)
        except ValueError as ve:
            LOGGER.warning('Line {}: Failed to parse value array entry {}!' \
                ' Using zero instead. - {}'.format(lineNo, entryNo, str(ve)))
            num = 0

        # shorten decimals after comma
        '''
        comSplit = numStr.split('.', 1)
        decAfterComma = int(DECIMALS_AFTER_COMMA)
        if len(comSplit) > 1 and decAfterComma >= 0:
            dec = decAfterComma if len(comSplit[1]) > decAfterComma else len(comSplit[1])
            numStr = comSplit[0] + '.' + comSplit[1][:dec]
        '''

        # quickly add zeros
        if num == 0 or num == -0:
            valuesArray.append(0.0)
            continue

        # round a bit at decimals after comma
        decAfterComma = int(DECIMALS_AFTER_COMMA)
        if decAfterComma >= 0:
            mulDiv = pow(10, decAfterComma) if decAfterComma > 0 else 0
            num = round(num * mulDiv) / mulDiv

        if num == -0 : num = 0.0
        valuesArray.append(num)


    # bring into useful format
    return {
        'file': filePath,
        'method': methodName,
        'values': valuesArray
    }


def findMethodPositionsJava(methodNames, filePath, filenameNoExt, debug=False):
    '''
    Find method position in a Java file.
    The method name "<init>" will be replaced by the
    file name to simply search for the constructors.
    See here: https://docs.oracle.com/javase/specs/jvms/se7/html/jvms-2.html#jvms-2.9

    Returns the method positions as a dictionary in format:
    - methodName: {from, to}

    Note: "to" must not always be given (e.g. if no method end could be found!)
    '''

    # validate file path
    if not os.path.exists(filePath) or not os.path.isfile(filePath):
        LOGGER.error('Failed to search for methods in file: {}'.format(os.path.abspath(filePath)))
        return None

    # read source code
    file = open(filePath, 'r')
    javaSourceCode = file.read()
    file.close()

    # parse source code
    tree = None
    try: tree = javalang.parse.parse(javaSourceCode)
    except Exception as ex:
        LOGGER.error('Failed to parse Java file: {}'.format(os.path.abspath(filePath)))
        return None

    # get available class
    javaClasses = []
    for t in tree.types:
        if isinstance(t, javalang.tree.ClassDeclaration):
            javaClasses.append(t)

    if len(javaClasses) < 1:
        LOGGER.error('Failed to find class in Java file: {}'.format(os.path.abspath(filePath)))
        return None

    elif len(javaClasses) > 1:
        LOGGER.warning('Multiple class instances in file: {} - Using first one.'.format(os.path.abspath(filePath)))

    # (only one class is supported currently - see given example data)
    jClass = javaClasses[0]

    # try to find method names
    fromLines = {}
    methodPositions = {}
    for method in methodNames:

        # treat "<init>" as constructor
        if method == "<init>":

            # search for constructors
            for e in jClass.body:
                if isinstance(e, javalang.tree.ConstructorDeclaration):
                    if not method in methodPositions:
                        methodPositions[method] = {'from': e.position.line}
                        fromLines[e.position.line] = True

        # get start line info of each method declaration
        for m in jClass.methods:
            if m.name != method: continue
            if not method in methodPositions:
                methodPositions[method] = {'from': m.position.line}
                fromLines[m.position.line] = True

    # find method offsets (somehow not given by javalang)
    with open(filePath, 'r') as file:

        lineNo = 0
        justStartedSearching = False
        searchingMethodEnd = False
        curMethod = None
        firstBracketFound = False
        scopesOpen = 0
        startLine = 0

        for line in file:

            # find the method
            lineNo += 1

            if not searchingMethodEnd:

                # skip line if there is no method starting here
                if not lineNo in fromLines: continue

                # a method is starting here
                for method in methodPositions:
                    fromLine = methodPositions[method]['from']
                    if fromLine == lineNo:
                        curMethod = method
                        searchingMethodEnd = True
                        firstBracketFound = False
                        scopesOpen = 0
                        methodLinesOffset = 0
                        startLine = lineNo
                        break

            # count scopes using opening and closing brackets
            for character in line:
                if character == "{":
                    scopesOpen += 1
                    firstBracketFound = True
                elif character == "}": scopesOpen -= 1

            # assume that this is the end of a method
            if (firstBracketFound and scopesOpen < 1) or (lineNo + 1) in fromLines:
                searchingMethodEnd = False
                methodPositions[method]['to'] = lineNo

    return methodPositions


if __name__ == '__main__':
    main()
