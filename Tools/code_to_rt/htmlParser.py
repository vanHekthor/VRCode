# Code by Leon H.
# github.com/S1r0hub

from html.parser import HTMLParser


class HtmlParser(HTMLParser):
    '''
    Parser to parse HTML code to the Rich Text Syntax of Unity.
    Uses the HTMLParse as base class.
    '''

    def __init__(self, colorSchema, set_default_color=False, default_color='#000000'):
        '''
        Parameters:
        - colorSchema: a set (each key matches a class of the HTML code) and a value is a color code
        - set_default_color: enable/disable setting default color for unknown classes
        - default_color: default color for unknown classes

        Use feed(htmlCode) to run the parser and use getRichText() to retrieve the parsed result.
        '''

        HTMLParser.__init__(self)

        # color schema (color value for each class)
        self.colorSchema = colorSchema

        # look for span-tags in the HTML code (these are the ones that interest us)
        self.tag = "span"

        # name of key to get the code color class from (e.g. <span class="o">...)
        self.key_class = 'class'
        
        # enable/disable setting default color for unknown classes
        self.set_default_color = set_default_color
        self.default_color = default_color

        # keys of the dictionary that will be passed to the callback function
        self.out_key_code = 'code'
        self.out_key_class = 'class'
        self.out_key_color = 'color'
        self.out_key_previous = 'prev'

        ### values of following variables will change on runtime

        # if inside the matching tag
        self.insideTag = False

        # the current element and its data (attrs and data)
        self.current = {}
        self.previousData = ''

        # final and parsed result
        self.result = ''


    def getRichText(self):
        '''
        Get the final and parsed result in Unity3D Rich Text format.
        For format information see: https://docs.unity3d.com/Manual/StyledText.html
        '''
        return self.result


    def handle_starttag(self, tag, attrs):
        #print('Start tag: {} - Attributes: {}'.format(tag, attrs))

        # if there is a span inside a span, the outer one wont be taken into account!
        if self.tagMatch(tag):
            self.insideTag = True
            self.current = {}

            # add color class of the following data
            if len(attrs) > 0 and self.key_class in attrs[0]:
                colorClass = attrs[0][1]
                self.current[self.out_key_class] = colorClass

                # get according color and add it
                colorValue = self.getColorFor(colorClass)
                if not colorValue is None:
                    self.current[self.out_key_color] = colorValue

                # add previous data (could be whitespace or line breaks)
                self.current[self.out_key_previous] = self.previousData


    def handle_endtag(self, tag):
        #print('End tag: {}'.format(tag))

        if self.tagMatch(tag) and self.out_key_class in self.current:
            self.addResult(self.current)

        # clear previous data
        self.previousData = ''


    def handle_data(self, data):
        #print('Data: {}'.format(data))

        if self.insideTag:
            if self.out_key_code in self.current:
                cur = self.current[self.out_key_code]
                self.current[self.out_key_code] = cur + data
            else:
                self.current[self.out_key_code] = data

        self.previousData = data


    def tagMatch(self, tag):
        ''' Returns true if this is a tag we search for. '''
        return tag.lower() == self.tag.lower()


    def getColorFor(self, colorClass):
        '''
        Returns the according color value or
        the default color (if enabled) value if the class is not known.
        If the set_default_color is False, None will be returned for unknown classes.
        '''

        if colorClass in self.colorSchema:
            return self.colorSchema[colorClass]

        if self.set_default_color:
            return self.default_color

        return None


    def addResult(self, currentElement):
        ''' Adds the current element to the final result. '''

        if currentElement is None:
            return

        if not self.out_key_code in currentElement:
            return

        # add previous data (e.g. whitespaces or line breaks)
        out = ''
        if self.out_key_previous in currentElement:
            out = currentElement[self.out_key_previous]

        # get color info if available
        colorInfo = None
        if self.out_key_color in currentElement:
            colorInfo = currentElement[self.out_key_color]

        # start tag of color
        if not colorInfo is None:
            out += '<color={}>'.format(colorInfo)

        out += currentElement[self.out_key_code]

        # end tag of color
        if not colorInfo is None:
            out += '</color>'

        # add this part to the final result
        self.result += out
