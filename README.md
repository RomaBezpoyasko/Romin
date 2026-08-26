# Romin

Romin is a simple embedded scripting language for .NET applications.

It is designed for C# developers who want to add scripting capabilities to their applications without learning a completely unfamiliar language.

Romin combines familiar C#/.NET syntax and objects, Python-style indentation, and Lua-style tables.

```romin
if user.IsActive
  print("User is active")
```

Create and use .NET objects directly:

```romin
client = new System.Net.WebClient()
```

The goal is simple: write scripts that feel natural to a C# developer, but are much shorter and easier to write.

## Why Romin?
Familiar to C# developers

Runs inside .NET applications

Direct access to .NET objects

Python-like indentation

Lua-like tables

Shorter scripts

Easy to embed


## Features

### Hello World

```romin
print("Hello, World!")
```

Prints text to the console or to the function provided by the host application.

### Variables

```romin
name = "John"
age = 30
weight = 19.95 // double value
price = 12.34m // decimal value
active = true

```

Variables are created by assignment. No unnecessary declarations are required.

### Conditions

```romin
if age >= 18
  print("Adult")
else
  print("Under 18")
```

Conditions use Python-style indentation instead of `{}`.

### Multiple Conditions

```romin
if score >= 90
  print("Excellent")
else if score >= 70
  print("Good")
else
  print("Try again")
```

Branches are easy to read and require no braces.

### While Loops

```romin
while count < 10
  print(count)
  count++
```

Repeats a block while the condition is true.

### For Loops

```romin
for item in items
  print(item)
```

Iterates through a collection.

### Ranges

```romin
for i in 1..10
  print(i)
```

Creates a range and iterates from 1 to 10.

### Functions

Functions are declared using `fn`.

```romin
fn add(a, b)
  return a + b

result = add(10, 20)
print(result)
```

Functions can accept parameters and return values.

### Function with Logic

```romin
fn getMessage(age)
  if age >= 18
    return "Adult"
  return "Under 18"

print(getMessage(25))
```

### Functions as Values

Functions can be treated as runtime values.

```romin
fn hello()
  print("Hello")

action = hello
action()
```

This allows functions to be passed around and used dynamically.
Functions can contain any normal Romin statements.

## Tables

Tables are one of the core data structures in Romin.

They use a compact syntax inspired by Lua.

### List Table

```romin
tab = [1, 2, 3]
```

A table can contain a sequence of values.

Access values by index:

```romin

//Index start from 1 like in Lua

print(tab[1])
print(tab[2])
print(tab[3])
```

### Named Table

Tables can also contain named fields:

```romin
tab2 = [Name:"Name", Age:23]
```

Fields can be accessed using dot notation:

```romin
print(tab2.Name)
print(tab2.Age)
```

This makes tables useful for simple objects and structured data.

### Tables with Different Values

```romin
user = [
  Name:"John",
  Age:23,
  Active:true
]
```

Tables can contain strings, numbers, booleans, other tables and objects.

### Nested Tables

```romin
user = [
  Name:"John",
  Address:[
    City:"London",
    Zip:12345
  ]
]

print(user.Address.City)
```

Tables can be nested to represent complex data.

## Null

```romin
value = null

if value == null
  print("No value")
```

Romin has an explicit null value.

## Null-Coalescing

```romin
name = user.name ?? "Unknown"
```

If `user.name` is null, `"Unknown"` is used instead.

## .NET Objects

One of Romin's main features is the ability to work with .NET objects directly.

```romin
client = new System.Net.WebClient()
```

The script can create and use objects from the .NET environment exposed by the host application.

This makes Romin especially natural for C# developers.

### Calling .NET Methods

Objects can be used in a familiar way:

```romin
result = object.DoSomething(value)
print(result)
```

Romin is designed to make interaction with host objects look natural to a C# programmer.

### Using Application Objects

A C# application can expose its own objects to Romin.

For example, the host application may provide:

```romin
printer.Print("Hello")
```

The script can therefore control functionality implemented in C#.

## Modules

Code can be organized into modules.

```romin
//parent.rn
fn square(x)
  return x * x
```
Inheritance

```romin
//child.rn
base 'parent.rn'

print(square(12))
```
or loaded like a module

```romin
//child2.rn
p = load('parent.rn')

print(p.square(12))
```

Modules help organize larger scripts and applications.

## Increment and Decrement

```romin
count++
count--
```

Useful for counters and loops.

## Arithmetic

```romin
total = price * quantity
average = total / count
difference = a - b
sum = a + b
```

Romin supports the standard arithmetic operators.

## Comparisons

```romin
if age >= 18
  print("Allowed")
```

Supported comparisons include:

```text
==
!=
<
>
<=
>=
```

## Logical Operators

```romin
if age >= 18 and active
  print("User can continue")
```

Logical operators can combine multiple conditions:

```text
and
or
```

## Interpolated string

```romin
name = "Mario"
text = $"Hello {name}"
print(text)
```
Romin is built around three ideas.

### Familiar to C# developers

.NET objects are used naturally:

```romin
use System.IO
file = new FileInfo("test.txt")
```

### Simple like Python

Blocks use indentation:

```romin
if condition
  doSomething()
```

No `{}` are required.

### Flexible like Lua

Tables are simple and powerful:

```romin
numbers = [1, 2, 3]

user = [
  Name:"John",
  Age:23
]
```

## Embedded .NET Scripting

Romin is primarily designed to be embedded inside .NET applications.

The host application controls the environment and can expose its own objects, functions and services.

```text
.NET Application
 │
 ├── Romin Runtime
 │
 ├── .NET Modules
 │
 ├── Application Objects
 │
 └── Built-in Functions
 │
 ▼
Romin Script
```

This makes Romin suitable for:

- application automation
- business rules
- workflows
- configuration
- plugins
- user-defined logic
- automation scripts
- application extensions

## Romin usefull example

```Romin
use 'System.Net'
use 'System.IO.Compression.ZipFile'

host = 'host'
login = 'login'
pass = 'pass'

fn get_ftp()
    client = new System.Net.WebClient()
    cred   = new System.Net.NetworkCredential(login, pass)
    client.Credentials = cred
    return client

fn upload_file(dst, src, ftp) 
    ftp = ftp ?? get_ftp()
    ftp.UploadFile(dst, src)

fn get_files(src, filter, all)
    return Directory.GetFiles(src, filter, all ? 
           SearchOption.AllDirectories: SearchOption.TopDirectoryOnly)

fn get_files_list(src, filter)
   d     = new DirectoryInfo(src)
   files = d.GetFiles()
   tab   = []
   for file in files
        if filter[file.Extension] != null
            tab[file.Name] = file.FullName        
   return tab

fn copy_files(src, dst, filter, all)
    files = get_files(src, filter, all)
    for file in files                    
        target = file.Replace(src, dst)
        dir    = Path.GetDirectoryName(target)             
        if !Directory.Exists(dir)
           Directory.CreateDirectory(dir) 
        File.Copy(file, target, true)

fn copy_all_files(src, dst, list_filter, all)
    for filter in list_filter
        copy_files(src, dst, filter, all)  

fn mkdir(name)
    return Directory.CreateDirectory(name)

fn copy_dir(src, dst)
   dirs =  Directory.GetDirectories(src, '*', 
                SearchOption.AllDirectories)    
   for dir in dirs
       mkdir(dir.Replace(src, dst))  

fn zip_file(dir, name) 
    return ZipFile.CreateFromDirectory(dir, name)
fn unzip_file(from, to)
    return ZipFile.ExtractToDirectory(from, to, true)

fn delete_file(name)
    return File.Delete(name)
fn delete_dir(name)
    return Directory.Delete(name, true)

```
