import json
import csv

with open('regions_performance.json') as f:
  data = json.load(f)

with open('model.csv', mode='w', newline='') as model_file:
    writer = csv.writer(model_file, delimiter=',', quotechar='"', quoting=csv.QUOTE_MINIMAL)

    writer.writerow(['id', 'root', 'blake2b', 'blake2b_1', 'gamma','dbg', 'brg', 'grg', 'sbrg', 'phi', 'garlic', 'lambda', 'v_id', 'd', 'hash', 'graph', 'performance(ms;<)'])
    for region in data['regions']:
        i = 0
        for influence in range(len(region['properties'][0]['value'])):
            optionList = [0 for x in range(len(region['properties'][0]['value']) + 2)]
            optionList[i] = 1
            if i == 0:
                writer.writerow([region['id']] + optionList + [region['properties'][0]['value'][i]])
            else:   
                writer.writerow([region['id']] + optionList + [region['properties'][0]['value'][i]])
                print("ID: " + region['id'] + "; Value: " + str(region['properties'][0]['value'][i]))    
            i = i + 1


        



