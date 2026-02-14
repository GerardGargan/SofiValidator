# Monthly Data Validation Console App

## Overview

This project is a .NET console application designed to automate previously manual processes used to validate monthly data submissions from multiple locations.

The legacy workflow relied heavily on manual Excel manipulation, which was time-consuming and error-prone. This application replaces that process by:

- Fetching source data directly from an API
- Parsing CSV data into strongly typed records
- Providing a menu-driven console interface
- Displaying validation results in a consistent, easy-to-review tabular format

The goal is to significantly reduce validation time while improving accuracy and repeatability.

---

## Key Features

- **API-Driven Data Retrieval**
  - Authenticated API call using a bearer token
  - CSV data ingestion using `CsvHelper`

- **Menu-Based Console Interface**
  - Interactive options to run specific validations
  - Clear, formatted console output for easy review

- **Automated Monthly Comparisons**
  - Automatically determines:
    - Current reporting month (previous calendar month)
    - Two prior comparison months
  - Indexes records for fast cross-month lookups

- **Validation Reports**
  - TRI (LTIs / MTIs) breakdown by site
  - Missing working hours detection with historical context
  - Lost Time Hours (LTH) analysis across multiple months

---

## Technical Stack

- **.NET Console Application**
- **C#**
- **CsvHelper** – CSV parsing
- **System.Globalization** – culture-safe formatting
- **HttpClient** – API access
- **DotNetEnv** – environment variable management

---

## Configuration

### Environment Variables

The application requires an API token to be set as an environment variable.

Create a `.env` file in the project root:

```env
KEY=your_api_token_here
