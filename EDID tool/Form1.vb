Imports System.IO.Ports
Public Class Form1
    Dim gridDATA(15, 15) As String ' data to show on DataGridView
    Dim binfile(256) As Byte ' data frome file
    Dim boardDATA(256) As Byte ' data frome board
    Dim filelen ' length of data from file
    Dim boardlen ' length of data from Board Data
    Dim DataSorce As String  'DataSorce of DataGridView
    Dim sp As New SerialPort()
    Dim readcommed As String = "E" ' request commed to read board data
    Dim Verify As Boolean = True ' status of Verify
    Dim COMportitem As Integer = 0
    Dim yyyy, ww
    Dim File_OK As Boolean = False 'resul of read file
    Dim Read_OK As Boolean = False 'resul of read board resul
    Dim CMD As String 'CMD to send
    Dim write_index As Integer = 0
    Dim delay_statu As Boolean = False
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim i
        Dim time As Date = Now
        yyyy = Year(Today)
        ww = DatePart("ww", Today)
        Label3.Text = "" ' reset 比對結果 text
        Label6.Text = "" ' reset EDID名稱 text
        Label8.Text = "" ' reset 輸入介面 text
        Label10.Text = "" ' reset 生產時間 text
        Label15.Text = "" ' reset 製造廠商 text
        Label16.Text = "" ' reset 建議解析度 text
        Label11.Text = yyyy.ToString & "年第" & ww.ToString & "週" ' set 現在時間 text
        Label13.Text = "軟體版本: " & My.Application.Info.Version.ToString ' Show version
        Timer1.Enabled = False
        Timer2.Enabled = False
        Button4.Enabled = False
        GetSerialPortNames() ' list available serial port dvices on bombobox1
        If COMportitem > 0 Then
            ComboBox1.SelectedIndex = 0 ' defaul first available serial port to use
        Else
            MsgBox("未偵測到COM Port", 48, "警告")
        End If
        For i = 0 To 255
            gridDATA(i \ 16, i Mod 16) = DECtoHEX(255) ' initial DataGridView data
        Next
        For i = 0 To 15

            DataGridView1.Columns.Add(i, DECtoHEX(i) & "h") ' add new Colums with Header

        Next i
        For i = 0 To 15
            ' add new rows
            DataGridView1.Rows.Add(gridDATA(i, 0), gridDATA(i, 1), gridDATA(i, 2), gridDATA(i, 3), gridDATA(i, 4), gridDATA(i, 5), gridDATA(i, 6), gridDATA(i, 7), gridDATA(i, 8), gridDATA(i, 9), gridDATA(i, 10), gridDATA(i, 11), gridDATA(i, 12), gridDATA(i, 13), gridDATA(i, 14), gridDATA(i, 15))
            DataGridView1.Rows(i).HeaderCell.Value = DECtoHEX(i) & "h" ' name Headers
        Next i
        DataGridView1.RowHeadersWidth = 60 'set header width to 60
        ' set serial ports up
        If COMportitem > 0 Then
            sp.Close()
            sp.PortName = ComboBox1.Items(ComboBox1.SelectedIndex)
            sp.BaudRate = 9600
        End If


    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        'button1 for read binfile
        Dim i, j, k, l
        Read_File()
        k = 0
        j = 0
        l = 0
        'verify if EDID is legal
        If File_OK Then
            For i = 0 To 127 ' 128 byte Checks SUM  
                If i < 8 Then
                    j = j + binfile(i) 'j = 00 FF FF FF FF FF FF 00
                End If
                k = k + binfile(i) ' Check SUM
            Next
            If binfile(20) < 128 And binfile(20) <> 104 Then
                ' show message VGA support sync error
                MsgBox("VGA 支援格式錯誤", 48, "警告")
            End If
            If binfile(126) = 1 Then
                    For i = 128 To 255
                        l = l + binfile(i)
                    Next
                End If
            If j = 1530 And k Mod 256 = 0 And l Mod 256 = 0 Then
                For i = 0 To 255
                    'Fill DataGridView byte data and /
                    gridDATA(i \ 16, i Mod 16) = DECtoHEX(binfile(i)) + "/"
                Next
                ' set up status
                DataSorce = "File"
                Button3.Enabled = True
                Button3.Text = "比對板子"
                Button4.Enabled = True
                PrintArr()
                Label3.Visible = False
                Label3.Text = ""
                GetInfomaction()
            ElseIf j <> 1530 Then
                ' show message EDID format ilegal
                MsgBox("EDID 起始格式錯誤", 48, "警告")
            ElseIf k Mod 256 <> 0 Then
                ' show message if 128 EDID is ilegal
                k = k Mod 256
                MsgBox("128 byte EDID不合法，Check SUM = " + k.ToString, 48, "警告")
            ElseIf l Mod 256 <> 0 Then
                ' show message if 128 EDID is ilegal
                l = l Mod 256
                    MsgBox("256 byte EDID不合法，Check SUM = " + l.ToString, 48, "警告")
                End If
                File_OK = False
            End If
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        'button2 for read EDID from A/D Board
        CMD = "R"
        Button2.Enabled = False
        Send_Command() ' send Board Data request by serial port
        Timer1.Enabled = True ' wate for response
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        ' button3 for compare EDID
        Dim i, j, k
        Label3.Text = "" ' reset 比對結果 text
        Label6.Text = "" ' reset EDID名稱 text
        Label8.Text = "" ' reset 輸入介面 text
        Label10.Text = "" ' reset 生產時間 text
        Label15.Text = "" ' reset 製造廠商 text
        Label16.Text = "" ' reset 建議解析度 text
        If DataSorce = "File" Then
            'if datasorce is file, Read Board data to compare
            Button3.Enabled = False
            CMD = "R"
            Send_Command()
            Timer2.Enabled = True

        ElseIf DataSorce = "Board" Then
            'if datasorce is file, Read binfile to compare
            Read_File()
            k = 0
            j = 0
            If File_OK Then
                'Fill DataGridView file data and board data
                For i = 0 To filelen - 1
                    If i < 8 Then
                        j = j + binfile(i) 'j = 00 FF FF FF FF FF FF 00
                    End If
                    k = k + binfile(i)
                Next
                'verify if EDID is legal
                If j = 1530 And k Mod 256 = 0 Then
                    'Fill DataGridView byte data and / board data
                    For i = 0 To 255
                        gridDATA(i \ 16, i Mod 16) = DECtoHEX(binfile(i)) + "/" + DECtoHEX(boardDATA(i))
                    Next
                    Verify = True
                    For i = 0 To boardlen - 1
                        If binfile(i) = boardDATA(i) Then
                            DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.ForeColor = Color.Black
                        Else
                            DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.ForeColor = Color.Red
                            Verify = False
                        End If
                    Next
                    ' setup status
                    DataSorce = "Board"
                    Button3.Text = "比對檔案"
                    Button4.Enabled = False
                    PrintArr()
                    Label3.Visible = True
                    ' show result of compare
                    If Verify Then
                        Label3.Text = "PASS"
                        Label3.ForeColor = Color.Green
                        GetInfomaction()
                    Else
                        Label3.Text = "False"
                        Label3.ForeColor = Color.Red
                    End If
                Else
                    ' show message if EDID is ilegal
                    k = k Mod 256
                    MsgBox("EDID不合法，Check SUM = " + k.ToString, 48, "警告")
                End If
                File_OK = False ' reset File_OK
            End If
        End If
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        'button4 for write EDID to A/D board
        ' disbale all buttons when writing EDID to A/D Board
        Button1.Enabled = False
        Button2.Enabled = False
        Button3.Enabled = False
        Button4.Enabled = False
        write_index = 0 'reset write index
        'send command to write EDID
        CMD = "W"
        Send_Command()
        Timer4.Enabled = True ' delay then recive command
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        'for button2, only read Board EDID and show in DataGridView1
        Dim i
        Timer1.Enabled = False
        If COMportitem > 0 Then Recive_Command()
        Delay()
        'fill board Data into DataGridView1 with setup
        For i = 0 To 255
            gridDATA(i \ 16, i Mod 16) = "/" + DECtoHEX(boardDATA(i))
            DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.ForeColor = Color.Black
        Next
        PrintArr()
        DataSorce = "Board"
        Button2.Enabled = True
        Button3.Text = "比對檔案"
        Button4.Enabled = False
        Label3.Visible = False
        Label3.Text = ""
    End Sub

    Private Sub Timer2_Tick(sender As Object, e As EventArgs) Handles Timer2.Tick
        'for button3, read Board EDID and show in DataGridView1 with binfile Data
        Dim i
        Timer2.Enabled = False
        If COMportitem > 0 Then Recive_Command()
        Verify = True
        Delay()
        'fill board Data and Board Data into DataGridView1 with setup
        For i = 0 To 255
            gridDATA(i \ 16, i Mod 16) = DECtoHEX(binfile(i)) + "/" + DECtoHEX(boardDATA(i))
        Next
        For i = 0 To filelen - 1 ' verify data area
            If binfile(i) = boardDATA(i) Then
                DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.ForeColor = Color.Black
            Else
                DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.ForeColor = Color.Red
                Verify = False
            End If
        Next

        PrintArr()
        ' setup status
        DataSorce = "File"
        Button3.Text = "比對板子"
        Button3.Enabled = True
        Button4.Enabled = True
        Label3.Visible = True
        ' show result of compare
        If Verify Then
            Label3.Text = "PASS"
            Label3.ForeColor = Color.Green
        Else
            Label3.Text = "False"
            Label3.ForeColor = Color.Red
        End If
    End Sub

    Private Sub Timer3_Tick(sender As Object, e As EventArgs) Handles Timer3.Tick
        'for Button4, just recive Board Data to Verify
        Timer3.Enabled = False
        Write_Verify()

    End Sub

    Private Sub Timer4_Tick(sender As Object, e As EventArgs) Handles Timer4.Tick
        ' delay then recive command
        Timer4.Enabled = False
        Recive_Command()
    End Sub

    Private Sub ComboBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox1.SelectedIndexChanged
        ' change to selected COM port 
        sp.Close()
        sp.PortName = ComboBox1.Items(ComboBox1.SelectedIndex)
    End Sub

    Sub GetSerialPortNames()
        ' Show all available COM ports.
        For Each sp As String In SerialPort.GetPortNames()
            ComboBox1.Items.Add(sp)
            COMportitem = COMportitem + 1
        Next
    End Sub

    Sub Read_File()
        Dim OpenFileDialog As New OpenFileDialog
        Dim FileName
        Dim SUMvalue As Integer = 0 ' value to check if 128-355 os null
        Dim i = 0
        'open file selector
        'OpenFileDialog1.InitialDirectory = My.Computer.FileSystem.SpecialDirectories.MyDocuments 'always open MyDocuments
        OpenFileDialog1.InitialDirectory = "" ' open floder that last time open
        OpenFileDialog1.Filter = "Bin files (*.bin)|*.bin|All files (*.*)|*.*"
        If (OpenFileDialog1.ShowDialog(Me) = System.Windows.Forms.DialogResult.OK) Then
            FileName = OpenFileDialog1.FileName
            Dim bindata() = My.Computer.FileSystem.ReadAllBytes(FileName) 'load bin file by byte
            filelen = bindata.Length ' get Data length
            'if length  under 256, put FF to The rest and set Grid Cell to  Gray
            If filelen < 256 Then
                Array.Resize(bindata, 256)
                For i = filelen To 255
                    bindata(i) = 255
                    DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.BackColor = Color.Gray
                    DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.ForeColor = Color.Black
                Next
                For i = 0 To filelen - 1
                    DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.BackColor = Color.White
                    DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.ForeColor = Color.Black
                Next
            Else
                For i = 0 To filelen - 1
                    DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.BackColor = Color.White
                    DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.ForeColor = Color.Black
                Next
            End If
            'check if byte 128 to 255 is null, make DataGridView cell gray
            For i = 128 To 255
                SUMvalue = SUMvalue + bindata(i)
            Next
            If SUMvalue = 32640 Or SUMvalue = 0 Then
                filelen = 128
                For i = 128 To 255
                    DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.BackColor = Color.Gray 'set Grid Cell to Gray
                    DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.ForeColor = Color.Black 'Set color of font Grid Cell To black
                Next
            End If
            'put DATA into binfile
            For i = 0 To 255
                binfile(i) = bindata(i)
            Next
            File_OK = True
        Else
            MsgBox("檔案未讀取", 48, "警告")
        End If
    End Sub

    Sub Send_Command()
        sp.Open()
        Dim i
        If COMportitem > 0 Then
            For i = 0 To 255
                boardDATA(i) = 255 ' defaul Board DATA into "FF"
            Next
            sp.Write(CMD)
        Else
            MsgBox("未偵測到COM Potr", 16, "警告")
        End If
    End Sub

    Sub Recive_Command()
        Dim i, j, k, l
        Dim Readbyte = sp.BytesToRead 'get numbers of COM port buffer
        Dim SUMvalue = 0
        Dim Template = 0
        j = 0
        k = 0
        l = 0
        'if recive EDID, save to boardDATA and print on DataGridView1
        If Readbyte > 128 And Readbyte <= 256 Then
            boardlen = Readbyte
            For i = 0 To 255
                boardDATA(i) = sp.ReadByte 'save Data to BoardDATA
                If i < 8 Then
                    j = j + boardDATA(i) 'j = 00 FF FF FF FF FF FF 00
                End If
                If i <= 127 Then
                    k = k + boardDATA(i) ' 128 byte Check SUM
                    DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.BackColor = Color.White 'set Grid Cell to white
                    DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.ForeColor = Color.Black 'Set color of font Grid Cell To black
                ElseIf i > 127 Then
                    l = l + boardDATA(i) ' 256 byte Check SUM
                End If
            Next
            ' if Board Data is legal, fill DATA normally
            If j = 1530 And k Mod 256 = 0 Then
                If boardDATA(20) < 128 And boardDATA(20) <> 104 Then
                    ' show message VGA support sync error
                    MsgBox("VGA 支援格式錯誤", 48, "警告")
                End If
                If boardDATA(126) = 0 Then
                    'if numbers of EDID is 128 byte, put FF to The rest and set Grid Cell to  Gray
                    For i = 128 To 255
                        boardDATA(i) = 255
                        DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.BackColor = Color.Gray 'set Grid Cell to Gray
                        DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.ForeColor = Color.Black 'Set color of font Grid Cell To black
                    Next
                    Read_OK = True
                ElseIf l Mod 256 = 0 Then
                    'if numbers of EDID is 256 byte, check the checkSUM and set Grid Cell to  Write
                    For i = 128 To 255
                        DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.BackColor = Color.White 'set Grid Cell to white
                        DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.ForeColor = Color.Black 'Set color of font Grid Cell To black
                    Next
                    Read_OK = True
                Else
                    ' numbers of EDID is 256 byte, and checkSUM is ilegal
                    Read_OK = False
                End If
                If Read_OK Then
                    DataSorce = "Board" 'DataGtidView prints Board Data
                    Button3.Enabled = True
                    Button3.Text = "比對檔案"
                    GetInfomaction()
                Else
                    ' show message if 256 EDID is ilegal
                    l = l Mod 256
                    MsgBox("EDID不合法，128-256 Check SUM = " + l.ToString)
                End If
            Else
                ' show message if 128 EDID is ilegal
                k = k Mod 256
                MsgBox("EDID不合法，Check SUM = " + k.ToString)
            End If
            sp.Close()
        ElseIf Readbyte = 1 Then
            'recive return commecd from Arduino
            Template = sp.ReadByte
            If Template = 87 Then ' ASCII code "W"
                'recive W , send next 32 byte to board 
                send32byte()
            ElseIf Template = 70 Then ' ASCII code "F"
                'recive F , board have recive 256 byte, finish transmission
                MsgBox("寫入完畢", 0)
                sp.Close()
                CMD = "R"
                Send_Command() ' send read command to read board data
                Timer3.Enabled = True 'verify written EDID
                'show other buttons
                Button1.Enabled = True
                Button2.Enabled = True
                Button3.Enabled = True
                Button4.Enabled = True
            Else
                'recive Unexpected word
                MsgBox(Template, 0, "收到意料外的資訊")
                sp.Close()
            End If
        Else
            ' didn't recive command or recive more then 256 bytes data
            MsgBox(Readbyte, 0, "收到意料外的資訊量")
            sp.Close()
        End If

    End Sub

    Sub send32byte()
        'send 32 byte to write
        If write_index < 256 Then
            sp.Write(binfile, write_index, 32)
            write_index = write_index + 32
            Timer4.Enabled = True 'delay then recive command
        End If
    End Sub

    Sub Write_Verify()
        'verify written data
        Dim i
        Dim Readbyte = sp.BytesToRead 'get numbers of COM port buffer
        Dim Template(255)
        Dim Verify_Resul As Boolean = True
        If Readbyte = 256 Then
            For i = 0 To 255
                Template(i) = sp.ReadByte 'read 256 bytes to BoardDATA
                If binfile(i) <> Template(i) Then Verify_Resul = False
            Next
        Else
            MsgBox(Readbyte, 48, "Readbyte ")
            MsgBox("EDID讀取錯誤", 48, "警告")
        End If
        If Verify_Resul Then
            MsgBox("EDID燒錄成功", 0, "EDID燒錄結果")
        Else
            MsgBox("EDID燒錄失敗", 48, "EDID燒錄結果")
        End If
        sp.Close()

    End Sub

    Sub PrintArr()
        ' show gridDATA on DataGridView1
        Dim i, j
        For i = 0 To 15
            For j = 0 To 15
                DataGridView1.Rows(i).Cells(j).value = gridDATA(i, j)
            Next
        Next
    End Sub

    Sub GetInfomaction()
        If DataSorce = "File" Then ' if data read from bin file then show binfile infomaction
            ' get Detailed Timing Descriptions
            Dim Hactive As Integer = 0
            Dim Hblankin As Integer = 0
            Dim Vactive As Integer = 0
            Dim Vblanking As Integer = 0
            Dim PXclock As Integer = 0
            Dim VF As Byte = 0
            PXclock = binfile(54) + (binfile(55) * 256)
            Hactive = binfile(56) + ((binfile(58) \ 16) * 256)
            Hblankin = binfile(57) + ((binfile(58) Mod 16) * 256)
            Vactive = binfile(59) + ((binfile(61) \ 16) * 256)
            Vblanking = binfile(60) + ((binfile(61) Mod 16) * 256)
            VF = (PXclock * 10000) / ((Hactive + Hblankin) * (Vactive + Vblanking))
            Label16.Text = Hactive.ToString & " x " & Vactive.ToString & " / " & VF.ToString & "Hz"
            Label16.BackColor = Color.Orange
            For i = 54 To 61
                DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.BackColor = Color.Orange
            Next
            'Get years and weeks of production
            Label10.Text = (binfile(17) + 1990).ToString & "年第" & binfile(16).ToString & "週"
            Label10.BackColor = Color.Yellow
            For i = 16 To 17
                DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.BackColor = Color.Yellow
            Next
            'Check signal souce
            If binfile(20) < 128 Then
                Label8.Text = "VGA"
            Else
                If binfile(126) = 0 Then
                    Label8.Text = "DVI"
                Else
                    If binfile(19) = 3 Then
                        Label8.Text = "HDMI"
                    ElseIf binfile(19) = 4 Then
                        Label8.Text = "DP"
                    End If
                End If
            End If
            Label8.BackColor = Color.LightGreen
            DataGridView1.Rows(1).Cells(4).Style.BackColor = Color.LightGreen
            If binfile(20) = 96 Then
                DataGridView1.Rows(1).Cells(4).Style.BackColor = Color.Red
            End If
            'get Manufacturer Name
            Dim MFTR_code As Integer
            Dim MFTR_name(3) As Byte
            MFTR_code = (binfile(8) * 256) + binfile(9)
            MFTR_name(0) = MFTR_code \ 1024
            MFTR_name(1) = (MFTR_code Mod 1024) \ 32
            MFTR_name(2) = MFTR_code Mod 32
            Label15.Text = DECtoEISA(MFTR_name(0)) & DECtoEISA(MFTR_name(1)) & DECtoEISA(MFTR_name(2))
            Label15.BackColor = Color.LightBlue
            DataGridView1.Rows(0).Cells(8).Style.BackColor = Color.LightBlue
            DataGridView1.Rows(0).Cells(9).Style.BackColor = Color.LightBlue
            'get Monitor name
            Dim first_digi As Byte
            If binfile(75) = 252 Then ' if 4Bh =  FCh, Monitor Descriptor2 is Monitor Name
                first_digi = 77
                'get monitor name from Monitor Descriptor2
                Label6.Text = Chr(binfile(first_digi)) & Chr(binfile(first_digi + 1)) & Chr(binfile(first_digi + 2)) & Chr(binfile(first_digi + 3)) & Chr(binfile(first_digi + 4)) & Chr(binfile(first_digi + 5)) & Chr(binfile(first_digi + 6)) & Chr(binfile(first_digi + 7)) & Chr(binfile(first_digi + 8)) & Chr(binfile(first_digi + 9)) & Chr(binfile(first_digi + 10)) & Chr(binfile(first_digi + 11)) & Chr(binfile(first_digi + 12))
                Label6.BackColor = Color.LightCyan
                For i = 75 To 75 + 14
                    DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.BackColor = Color.LightCyan
                Next
            End If
            If binfile(93) = 252 Then ' if 5Dh =  FCh, Monitor Descriptor3 is Monitor Name
                first_digi = 95
                'get monitor name from Monitor Descriptor3
                Label6.Text = Chr(binfile(first_digi)) & Chr(binfile(first_digi + 1)) & Chr(binfile(first_digi + 2)) & Chr(binfile(first_digi + 3)) & Chr(binfile(first_digi + 4)) & Chr(binfile(first_digi + 5)) & Chr(binfile(first_digi + 6)) & Chr(binfile(first_digi + 7)) & Chr(binfile(first_digi + 8)) & Chr(binfile(first_digi + 9)) & Chr(binfile(first_digi + 10)) & Chr(binfile(first_digi + 11)) & Chr(binfile(first_digi + 12))
                Label6.BackColor = Color.LightCyan
                For i = 93 To 93 + 14
                    DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.BackColor = Color.LightCyan
                Next
            End If
            If binfile(111) = 252 Then ' if 6Fh =  FCh, Monitor Descriptor4 is Monitor Name
                first_digi = 113
                'get monitor name from Monitor Descriptor4
                Label6.Text = Chr(binfile(first_digi)) & Chr(binfile(first_digi + 1)) & Chr(binfile(first_digi + 2)) & Chr(binfile(first_digi + 3)) & Chr(binfile(first_digi + 4)) & Chr(binfile(first_digi + 5)) & Chr(binfile(first_digi + 6)) & Chr(binfile(first_digi + 7)) & Chr(binfile(first_digi + 8)) & Chr(binfile(first_digi + 9)) & Chr(binfile(first_digi + 10)) & Chr(binfile(first_digi + 11)) & Chr(binfile(first_digi + 12))
                Label6.BackColor = Color.LightCyan
                For i = 111 To 111 + 14
                    DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.BackColor = Color.LightCyan
                Next
            End If
        ElseIf DataSorce = "Board" Then ' if data read from Board then show board infomaction
            ' get Detailed Timing Descriptions
            Dim Hactive As Integer = 0
            Dim Hblankin As Integer = 0
            Dim Vactive As Integer = 0
            Dim Vblanking As Integer = 0
            Dim PXclock As Integer = 0
            Dim VF As Byte = 0
            Hactive = boardDATA(56) + ((boardDATA(58) \ 16) * 256)
            Hblankin = boardDATA(57) + ((boardDATA(58) Mod 16) * 256)
            Vactive = boardDATA(59) + ((boardDATA(61) \ 16) * 256)
            Vblanking = boardDATA(60) + ((boardDATA(61) Mod 16) * 256)
            PXclock = boardDATA(54) + (boardDATA(55) * 256)
            VF = (PXclock * 10000) / ((Hactive + Hblankin) * (Vactive + Vblanking))
            Label16.Text = Hactive.ToString & " x " & Vactive.ToString & " / " & VF.ToString & "Hz"
            Label16.BackColor = Color.Orange
            For i = 54 To 61
                DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.BackColor = Color.Orange
            Next
            'Get years and weeks of production
            Label10.Text = (boardDATA(17) + 1990).ToString & "年第" & boardDATA(16).ToString & "週"
            Label10.BackColor = Color.Yellow
            For i = 16 To 17
                DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.BackColor = Color.Yellow
            Next
            'Check signal souce
            If boardDATA(20) < 128 Then
                Label8.Text = "VGA"
            Else
                If boardDATA(126) = 0 Then
                    Label8.Text = "DVI"
                Else
                    If boardDATA(19) = 3 Then
                        Label8.Text = "HDMI"
                    ElseIf boardDATA(19) = 4 Then
                        Label8.Text = "DP"
                    End If
                End If
            End If
            Label8.BackColor = Color.LightGreen
            DataGridView1.Rows(1).Cells(4).Style.BackColor = Color.LightGreen
            If boardDATA(20) = 96 Then
                DataGridView1.Rows(1).Cells(4).Style.BackColor = Color.Red
            End If
            'get Manufacturer Name
            Dim MFTR_code As Integer
            Dim MFTR_name(3) As Byte
            MFTR_code = (boardDATA(8) * 256) + boardDATA(9)
            MFTR_name(0) = MFTR_code \ 1024
            MFTR_name(1) = (MFTR_code Mod 1024) \ 32
            MFTR_name(2) = MFTR_code Mod 32
            Label15.Text = DECtoEISA(MFTR_name(0)) & DECtoEISA(MFTR_name(1)) & DECtoEISA(MFTR_name(2))
            Label15.BackColor = Color.LightBlue
            DataGridView1.Rows(0).Cells(8).Style.BackColor = Color.LightBlue
            DataGridView1.Rows(0).Cells(9).Style.BackColor = Color.LightBlue
            'get Monitor name
            Dim first_digi As Byte
            If boardDATA(75) = 252 Then ' if 4Bh =  FCh, Monitor Descriptor2 is Monitor Name
                first_digi = 77
                'get monitor name from Monitor Descriptor2
                Label6.Text = Chr(boardDATA(first_digi)) & Chr(boardDATA(first_digi + 1)) & Chr(boardDATA(first_digi + 2)) & Chr(boardDATA(first_digi + 3)) & Chr(boardDATA(first_digi + 4)) & Chr(boardDATA(first_digi + 5)) & Chr(boardDATA(first_digi + 6)) & Chr(boardDATA(first_digi + 7)) & Chr(boardDATA(first_digi + 8)) & Chr(boardDATA(first_digi + 9)) & Chr(boardDATA(first_digi + 10)) & Chr(boardDATA(first_digi + 11)) & Chr(boardDATA(first_digi + 12))
                Label6.BackColor = Color.LightCyan
                For i = 75 To 75 + 14
                    DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.BackColor = Color.LightCyan
                Next
            End If
            If boardDATA(93) = 252 Then ' if 5Dh =  FCh, Monitor Descriptor3 is Monitor Name
                first_digi = 95
                'get monitor name from Monitor Descriptor3
                Label6.Text = Chr(boardDATA(first_digi)) & Chr(boardDATA(first_digi + 1)) & Chr(boardDATA(first_digi + 2)) & Chr(boardDATA(first_digi + 3)) & Chr(boardDATA(first_digi + 4)) & Chr(boardDATA(first_digi + 5)) & Chr(boardDATA(first_digi + 6)) & Chr(boardDATA(first_digi + 7)) & Chr(boardDATA(first_digi + 8)) & Chr(boardDATA(first_digi + 9)) & Chr(boardDATA(first_digi + 10)) & Chr(boardDATA(first_digi + 11)) & Chr(boardDATA(first_digi + 12))
                Label6.BackColor = Color.LightCyan
                For i = 93 To 93 + 14
                    DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.BackColor = Color.LightCyan
                Next
            End If
            If boardDATA(111) = 252 Then ' if 6Fh =  FCh, Monitor Descriptor4 is Monitor Name
                first_digi = 113
                'get monitor name from Monitor Descriptor4
                Label6.Text = Chr(boardDATA(first_digi)) & Chr(boardDATA(first_digi + 1)) & Chr(boardDATA(first_digi + 2)) & Chr(boardDATA(first_digi + 3)) & Chr(boardDATA(first_digi + 4)) & Chr(boardDATA(first_digi + 5)) & Chr(boardDATA(first_digi + 6)) & Chr(boardDATA(first_digi + 7)) & Chr(boardDATA(first_digi + 8)) & Chr(boardDATA(first_digi + 9)) & Chr(boardDATA(first_digi + 10)) & Chr(boardDATA(first_digi + 11)) & Chr(boardDATA(first_digi + 12))
                Label6.BackColor = Color.LightCyan
                For i = 111 To 111 + 14
                    DataGridView1.Rows(i \ 16).Cells(i Mod 16).Style.BackColor = Color.LightCyan
                Next
            End If
        End If

    End Sub

    Sub Delay()
        Dim i, j, k
        For i = 0 To 1024
            For j = 0 To 1024
                k = i + j
            Next
        Next

    End Sub

    Function DECtoHEX(value As Byte)
        Dim HEX As String ' target to Conversion (hight digit and low digit)
        Dim DH As String ' high Digit
        Dim DL As String ' low Digit
        DH = value \ 16
        ' tunes 10-15 to A-F
        Select Case value \ 16
            Case 10
                DH = "A"
            Case 11
                DH = "B"
            Case 12
                DH = "C"
            Case 13
                DH = "D"
            Case 14
                DH = "E"
            Case 15
                DH = "F"
        End Select

        DL = value Mod 16
        'tunes 10 - 15 to A-F
        Select Case value Mod 16
            Case 10
                DL = "A"
            Case 11
                DL = "B"
            Case 12
                DL = "C"
            Case 13
                DL = "D"
            Case 14
                DL = "E"
            Case 15
                DL = "F"
        End Select
        HEX = DH + DL
        Return HEX
    End Function

    Function HEXtoDEC(value As String)
        Dim DEC As Byte ' target to Conversion (hight digit and low digit)
        Dim DH As Byte ' high Digit
        Dim DL As Byte ' low Digit
        ' tunes  A-F to 10-15 

        Select Case Microsoft.VisualBasic.Left(value, 1)
            Case "A"
                DH = 10
            Case "B"
                DH = 11
            Case "C"
                DH = 12
            Case "D"
                DH = 13
            Case "E"
                DH = 14
            Case "F"
                DH = 15
            Case Else
                DH = Microsoft.VisualBasic.Left(value, 1)
        End Select


        'tunes 10 - 15 to A-F
        Select Case Microsoft.VisualBasic.Right(value, 1)
            Case "A"
                DL = 10
            Case "B"
                DL = 11
            Case "C"
                DL = 12
            Case "D"
                DL = 13
            Case "E"
                DL = 14
            Case "F"
                DL = 15
            Case Else
                DL = Microsoft.VisualBasic.Right(value, 1)
        End Select
        DEC = (DH * 16) + DL
        Return DEC
    End Function

    Function DECtoEISA(value As Byte)
        Dim EISA As String ' target to Conversion (hight digit and low digit)
        ' tunes 10-15 to A-F
        Select Case value
            Case 1
                EISA = "A"
            Case 2
                EISA = "B"
            Case 3
                EISA = "C"
            Case 4
                EISA = "D"
            Case 5
                EISA = "E"
            Case 6
                EISA = "F"
            Case 7
                EISA = "G"
            Case 8
                EISA = "H"
            Case 9
                EISA = "I"
            Case 10
                EISA = "J"
            Case 11
                EISA = "K"
            Case 12
                EISA = "L"
            Case 13
                EISA = "M"
            Case 14
                EISA = "N"
            Case 15
                EISA = "O"
            Case 16
                EISA = "P"
            Case 17
                EISA = "Q"
            Case 18
                EISA = "R"
            Case 19
                EISA = "S"
            Case 20
                EISA = "T"
            Case 21
                EISA = "U"
            Case 22
                EISA = "V"
            Case 23
                EISA = "W"
            Case 24
                EISA = "X"
            Case 25
                EISA = "Y"
            Case 26
                EISA = "Z"
            Case Else
                EISA = " "
        End Select
        Return EISA
    End Function
End Class