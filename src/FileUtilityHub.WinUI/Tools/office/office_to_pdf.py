import sys
import os
import win32com.client
from pathlib import Path
import traceback

def word_to_pdf(input_path, output_path):
    word = None
    doc = None
    try:
        word = win32com.client.Dispatch("Word.Application")
        word.Visible = False
        word.DisplayAlerts = 0 # wdAlertsNone
        doc = word.Documents.Open(input_path, ReadOnly=True)
        # wdExportFormatPDF = 17
        doc.ExportAsFixedFormat(output_path, 17)
    finally:
        if doc:
            try:
                doc.Close(SaveChanges=0) # wdDoNotSaveChanges
            except:
                pass
        if word:
            try:
                word.Quit()
            except:
                pass

def excel_to_pdf(input_path, output_path):
    excel = None
    wb = None
    try:
        excel = win32com.client.Dispatch("Excel.Application")
        excel.Visible = False
        excel.DisplayAlerts = False
        wb = excel.Workbooks.Open(input_path, ReadOnly=True)
        # xlTypePDF = 0
        wb.ExportAsFixedFormat(0, output_path)
    finally:
        if wb:
            try:
                wb.Close(SaveChanges=False)
            except:
                pass
        if excel:
            try:
                excel.Quit()
            except:
                pass

def ppt_to_pdf(input_path, output_path):
    ppt = None
    pres = None
    try:
        ppt = win32com.client.Dispatch("PowerPoint.Application")
        # PowerPoint API can be tricky with headless. WithWindow=msoFalse
        # ppWindowNone = 2, msoFalse = 0
        pres = ppt.Presentations.Open(input_path, ReadOnly=True, WithWindow=False)
        # ppSaveAsPDF = 32
        pres.SaveAs(output_path, 32)
    finally:
        if pres:
            try:
                pres.Close()
            except:
                pass
        if ppt:
            try:
                ppt.Quit()
            except:
                pass

def main():
    if len(sys.argv) < 3:
        print("Usage: python office_to_pdf.py <input_file> <output_folder>")
        sys.exit(1)

    input_file = os.path.abspath(sys.argv[1])
    output_folder = os.path.abspath(sys.argv[2])

    if not os.path.exists(input_file):
        print(f"Error: Input file does not exist: {input_file}")
        sys.exit(1)

    if not os.path.exists(output_folder):
        os.makedirs(output_folder, exist_ok=True)

    input_path = Path(input_file)
    ext = input_path.suffix.lower()
    output_pdf = os.path.join(output_folder, f"{input_path.stem}_{os.urandom(4).hex()}.pdf")

    try:
        if ext in ['.doc', '.docx']:
            word_to_pdf(input_file, output_pdf)
        elif ext in ['.xls', '.xlsx']:
            excel_to_pdf(input_file, output_pdf)
        elif ext in ['.ppt', '.pptx']:
            ppt_to_pdf(input_file, output_pdf)
        else:
            print(f"Error: Unsupported file extension: {ext}")
            sys.exit(1)
        
        # Verify output exists
        if os.path.exists(output_pdf):
            print(output_pdf) # Print exactly the output path for C# to read
            sys.exit(0)
        else:
            print("Error: Conversion completed but output PDF not found.")
            sys.exit(1)

    except Exception as e:
        print(f"COM Error: {str(e)}")
        traceback.print_exc()
        sys.exit(1)

if __name__ == "__main__":
    main()
