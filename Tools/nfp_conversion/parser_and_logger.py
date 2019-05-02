# Code by Leon H.
# github.com/S1r0hub
#
# To prepare the argument parser and logger.

import argparse
import logging


def prepareParser(description):
    ''' Prepares and returns the argument parser, adding required arguments to it. '''

    # create argument parser
    parser = argparse.ArgumentParser(
        description=description,
        formatter_class=argparse.ArgumentDefaultsHelpFormatter
    )

    parser.add_argument('-mp', '-mpath', '--measurements_path', required=True, type=str,
        help='Path to the file with measurements (one line equals one entry)')

    parser.add_argument('-pp', '-ppath', '--program_path', required=True, type=str,
        help='Path to a folder that contains all required program files (e.g. "src" of "src/main/java...")')

    parser.add_argument('-op', '-opath', '--outpath', required=True, type=str,
        help='Path of a folder to export the region files to')

    parser.add_argument('-on', '-oname', '--outname', required=False, type=str, default='converted.json',
        help='Name of the file to export results to')

    parser.add_argument('-sce', '-sc_extension', '--source_code_extension', required=False, type=str, default=".java",
        help='Extension of the according source code files in lower case!')

    parser.add_argument('-pn', '-pname', '--property_name', required=False, type=str, default="performance",
        help='Name of the region property that the values represent')

    parser.add_argument('-ni', '-nindentation', '--no_indentation', required=False, action='store_true',
        help='Exports the result JSON content as a single line (ugly but smaller file size)')

    parser.add_argument('-lf', '-logfile', '--logfile', required=False, type=str, default="logging",
        help='Path and name of the log file. Set empty to disable logging to a file')

    parser.add_argument('-ow', '-overwrite', '--overwrite', required=False, action='store_true',
        help='Add this flag to overwrite output files that already exist')

    parser.add_argument('-v', '-verbose', '--verbose', required=False, action='store_true',
        help='Add this flag for verbose output (debug logging enabled)')

    return parser


def prepareLogger(name, logPath, verboseLogging=False):
    ''' Prepares and returns the logger. '''

    # set up logger
    logger = logging.getLogger(name)
    logger.setLevel(logging.DEBUG)

    # check if debug should be enabled
    logLevel = logging.INFO
    if verboseLogging: logLevel = logging.DEBUG

    # channel to stream log events to console
    ch = logging.StreamHandler()
    ch.setLevel(logLevel)
    formatter = logging.Formatter('[%(levelname)s] (%(asctime)s): %(message)s')
    ch.setFormatter(formatter)
    logger.addHandler(ch)

    # log to file if enabled
    if len(logPath) > 0:
        if not logPath.endswith(".log"):
            logPath += ".log"
        fileHandler = logging.FileHandler(logPath)
        fileHandler.setFormatter(formatter)
        logger.addHandler(fileHandler)
    logger.info('Logger ready.')

    return logger
