;;;==========================================================================================
;;; “源泉设计” 启动程序 yqstart.lsp
;;;------------------------------------------------------------------------------------------
;;; 注：1. 这是“源泉设计”的启动程序，通常由sys\acad.lsp自动加载。假如acad.lsp与你的其他
;;;        程序产生冲突，令“源泉设计”未能自动加载，请将sys\acad.lsp删除，并请将本文件
;;;        yqstart.lsp 加入AutoCAD启动组；
;;;     2. 代码行中分号";"后面是注释，行首加分号则暂时取消该行；
;;;     3. 请不要修改本文件，除非您熟悉AutoLISP；
;;;     4. “源泉设计”v5.8.1版以后只能运行在AutoCAD 2000以上版，不再支持AutoCAD r14版了。
;;;==========================================================================================
(setq old_cmdecho (getvar "CMDECHO"))
(setvar "CMDECHO" 0)
(if (getvar "SECURELOAD") (setvar "SECURELOAD" 0))       ; AutoCAD 2014版 加载lisp不警告

(if (null c:yq_about)(progn
(if (or (and (getvar "GCADVER") (<= "13.0" (substr (getvar "GCADVER") 2 4)))
        (<= "15.0" (substr (getvar "ACADVER") 1 4))
    )
(progn


;;; 加载“源泉设计”程序核心
(if (and (null c:yq_about)(findfile "yqarch.vlx"))(load "yqarch.vlx"))


;;; 将 acad.lsp 文件加载到每一个图形
(if (getvar "acadlspasdoc") (setvar "acadlspasdoc" 1))


;;; 在屏幕下状态行显示当前“标注比例 DIMSCALE”“标注样式 DIMSTY”“文字样式 STYLE”
;(setvar "MODEMACRO" (strcat "DIMSCALE:<1:" "$(substr,$(getvar,DIMSCALE),1,6)"
;    "> DIMSTY:<" "$(getvar,DIMSTYLE)" "> STYLE:<" "$(getvar,TEXTSTYLE)" ">"))


;;; 加载源泉系统面板程序
(if (findfile "yqpanel.lsp") (load "yqpanel.lsp"))


;;; 运行源泉设计系统目录下“library”文件夹的程序文件
(if (and yq_library (/= yq_library ""))
    (progn
        (princ (yq_ec (strcat "\nLoading .lsp files in folder <" yq_library "\\>...")
                      (strcat "\n正在加载公用文件夹 <" yq_library "\\> 中的程序...")))
        (yq_run yq_library "*.lsp,*.vlx,*.fas")
        (princ "\n...")
    )
)


;;; 运行源泉设计系统目录下“当前用户”文件夹的程序文件
(if (and yq_user (/= yq_user ""))
    (progn
        (princ (yq_ec (strcat "\n[YQArch] Current User is <" yq_user ">.")
                      (strcat "\n[源泉设计]当前用户是 <" yq_user ">.")))
        (princ (yq_ec (strcat "\nLoading .lsp files in folder <" yq_user "\\>...")
                      (strcat "\n正在加载用户文件夹 <" yq_user "\\> 中的程序...")))
        (yq_runsc)             ; 命令快捷键设定
        (yq_yqpanel)           ; 系统面板设定
        (yq_runpanel)          ; 用户面板设定
        (yq_runlaysc)          ; 设置转换图层快捷键
        (yq_runwlaysc)         ; 设置转换通配符图层快捷键
        (yq_runltpsc)          ; 设置转换线型快捷键
        (yq_runhatsc)          ; 设置填充图案快捷键
        (yq_run yq_user "*.lsp,*.vlx,*.fas")
        (princ "\n...")
    )
)


;;; 加载源泉菜单为 AutoCAD 原菜单的最后一项
(if (not (menugroup "yqarch")) (c:yq_menu))


(princ (yq_ec "\nYQArch <yqstart.lsp> loaded! Enter 'yqarch' to launch\n\n"
              "\n源泉设计启动文件 <yqstart.lsp> 载入完毕!\n加载源泉菜单请键入 'yq_menu', 命令总览请键入 'yqarch'\n\n"))


)(princ (yq_ec "\nAvailable only at AutoCAD2000/GstarCAD8 or later!" "\n请在AutoCAD2000/浩辰CAD8以上版运行源泉设计."))
)
)
)

(if (and c:yq_about (not (menugroup "yqarch"))) (c:yq_menu))

(if old_cmdecho (progn (setvar "CMDECHO" old_cmdecho) (setq old_cmdecho nil)))
(princ)
