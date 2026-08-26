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
file = new System.IO.File()
```

The goal is simple: write scripts that feel natural to a C# developer, but are much shorter and easier to write.

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
price = 19.95
active = true

// decimal value
price = 12.345M
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
print(tab[0])
print(tab[1])
print(tab[2])
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

## Safe Navigation

```romin
city = user?.address?.city
```

Safe navigation allows nested objects to be accessed without immediately failing when an intermediate value is null.

It can be combined with `??`:

```romin
city = user?.address?.city ?? "Unknown"
```

## .NET Objects

One of Romin's main features is the ability to work with .NET objects directly.

```romin
file = new System.IO.File()
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
database.Query("SELECT * FROM Users")
```

or:

```romin
printer.Print("Hello")
```

The script can therefore control functionality implemented in C#.

## Modules

Code can be organized into modules.

```romin
module Math

function square(x)
  return x * x
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
not
```

## Combining Features

Romin becomes especially useful when several simple features are combined:

```romin
use System.IO

fn getSize(file)
  if file.Exists
    return file.Length
  return 0

files = [
  new FileInfo("one.txt"),
  new FileInfo("two.txt")
]

for file in files
  print(getSize(file))
```

This example combines:

- .NET modules
- .NET objects
- `new`
- functions
- conditions
- tables
- loops
- properties
- method/object access

## A More Realistic Example

```romin
use System.IO

fn processFile(path)
  file = new FileInfo(path)

  if !file.Exists
    print("File not found")
    return

  print("File: " + file.Name)
  print("Size: " + file.Length)

files = ["one.txt", "two.txt", "three.txt"]

for path in files
  processFile(path)
```

The script uses .NET classes while keeping the syntax compact and readable.

## Why Romin?

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

### Example: Application Scripting

A C# application could expose:

```text
printer
database
logger
configuration
```

A Romin script could then use them:

```romin
if configuration.PrintEnabled
  printer.Print("Hello")
logger.Info("Printing completed")
```

The application remains written in C#, while customizable logic can be written in Romin.

## In One Example

A small but realistic Romin script can look like this:

```romin
use System.IO

users = [
  [Name:"John", Age:25],
  [Name:"Mary", Age:17]
]

fn showUser(user)
  if user?.Age >= 18
    print("Adult: " + user.Name)
  else
    print("Under 18: " + user.Name)

for user in users
  showUser(user)
```

This demonstrates the main philosophy of Romin:

> simple syntax + .NET objects + Python-style blocks + Lua-style tables.

## Romin in One Sentence

Romin is a simple embedded scripting language for .NET that combines C#-style object usage, Python-style indentation and Lua-style tables.

```romin
use System.IO

file = new FileInfo("test.txt")

if file?.Exists
  print(file.Name)
```

Simple to write. Familiar to C# developers. Designed for .NET applications.
