/// Copyright (c) 2012 Ecma International.  All rights reserved. 
/**
 * @path ch15/15.2/15.2.3/15.2.3.6/15.2.3.6-4-236.js
 * @description Object.defineProperty - 'O' is an Array, 'name' is an array index property, the [[Configurable]] field of 'desc' and the [[Configurable]] attribute value of 'name' are two booleans with same value  (15.4.5.1 step 4.c)
 */


function testcase() {
        var arrObj = [];

        Object.defineProperty(arrObj, "0", { configurable: false });

        Object.defineProperty(arrObj, "0", { configurable: false });
        return dataPropertyAttributesAreCorrect(arrObj, "0", undefined, false, false, false);
    }
runTestCase(testcase);