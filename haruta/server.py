# -*- coding:utf-8 -*-
import socket
import psycopg2
import binascii
import webbrowser

psyconfig = "host=localhost dbname=wine user=haruta password=******"
con = psycopg2.connect(psyconfig)
cur = con.cursor()

host = "192.168.100.85" # 自分のIPを入れる
port = 65000 # RFIDリーダ側と共有する値

# おまじない
serversock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
serversock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
# 以下のコードでこのプログラムにport番号を割り当てるイメージ
serversock.bind((host,port))
serversock.listen(10) # 接続の待ち受けをします（キューの最大数を指定）

print 'Waiting for connections...'
# クライアントのソケットから接続要求がくれば値が入る
clientsock, client_address = serversock.accept() #接続されればデータを格納

previous_rcvmsg = 'chinpan' # init
while True:
    rcvmsg = clientsock.recv(1024)
    if rcvmsg != previous_rcvmsg:
        EPC = binascii.unhexlify(rcvmsg) # 16 -> ASCII
        print EPC
        sql = 'select url from wine_database where epc = \'' + str(EPC) + '\''
        cur.execute(sql)
        res = cur.fetchall()
        try:
            url = res[0][0]
            webbrowser.open_new_tab(url)
        except:
            print 'No result found.'

    previous_rcvmsg = rcvmsg
    #  time.sleep(3)

cur.close()
con.close()
clientsock.close()
