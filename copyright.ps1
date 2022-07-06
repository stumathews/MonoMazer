param($target = "D:\repos\MonoMazer", $companyname = "Stuart Mathews", $date = (Get-Date))

$header = "//-----------------------------------------------------------------------

// <copyright file=""{0}"" company=""{1}"">

// Copyright (c) {1}. All rights reserved.

// <author>Stuart Mathews</author>

// <date>{2}</date>

// </copyright>

//-----------------------------------------------------------------------`r`n"

function Write-Header ($file)
{
    $content = Get-Content $file

    $filename = Split-Path -Leaf $file

    $fileheader = $header -f $filename,$companyname,$date

    Set-Content $file $fileheader

    Add-Content $file $content
}

Get-ChildItem $target -Recurse | ? { $_.Extension -like ".cs" } | % `
{
    Write-Header $_.PSPath.Split(":", 3)[2]
}