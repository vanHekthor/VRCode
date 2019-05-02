#!/usr/bin/env python3
# This simple script converts the values (given in ns) in milliseconds.

FROMLINE = 1
TOLINE = 7
FILEPATH = "values_all.txt"

def main():

    with open(FILEPATH, "r") as file:

        lineNo = 0
        for line in file:

            lineNo += 1
            if lineNo < FROMLINE: continue
            if lineNo > TOLINE: break

            lineSplit = line.split('[', 1)
            arr = lineSplit[1].split(']', 1)[0]
            print("LINE {}: [{}]".format(lineNo, arr))

            numbers = [float(num.strip()) for num in arr.split(',')]
            converted = "["
            for num in numbers: converted += convertStr(num) + ", "
            converted = converted.strip()[:-1]
            converted += "]"
            print("CONV {}: {}".format(lineNo, converted))

def convertStr(num):
    if num == 0.0: return '0.0'
    return '{:.13f}'.format(num / 1000000.0)

if __name__ == '__main__':
    main()
