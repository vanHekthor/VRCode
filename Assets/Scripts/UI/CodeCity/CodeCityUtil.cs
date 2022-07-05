using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Spawner.CodeCity;

public class CodeCityUtil {
    public static CodeCityElement FindCodeCityElementWithPath(Transform codeCity, string relativePath) {
        var codeCityElement = codeCity.transform.Find(relativePath).GetComponent<CodeCityElement>();
        if (codeCityElement == null) {
            Debug.LogError($"Failed to find code city element with relative path '{relativePath}'!");
        }

        return codeCityElement;
    }
}
