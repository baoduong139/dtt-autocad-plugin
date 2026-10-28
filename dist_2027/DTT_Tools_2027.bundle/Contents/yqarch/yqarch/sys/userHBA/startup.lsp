;;;=========================================================================
;;;         源 泉 设 计    图档初始化文件，用于打造一个适合自己的运行环境
;;;-------------------------------------------------------------------------
;;; 文件夹 sys\用户文件夹\
;;;
;;;     “用户文件夹”为自建文件夹。将自编或收集到的“.lsp/.vlx/.arx”程序
;;; 文件放入此文件夹下，再设定此“用户文件夹”为当前用户(用命令yq_setuser)，
;;; 则“源泉建筑”在启动的时候会自动加载这些程序。
;;;=========================================================================
;;; 注：1. 此文件是用户图档初始化文件，用于打造一个适合自己的运行环境；
;;;     2. 代码行中分号";"后面是注释，行首加分号则暂时取消该行。

;;; 代码编写: Walter Lam   QQ: 575825448
;;; 复制传播本文件请保留以上信息



;;;----------------------------------------
;;; 加载docbar图形文档切换标签(如果有的话)
;;;----------------------------------------
(setq tmp_ver (substr (getvar "ACADVER") 1 4)
      tmp_str (cond ((= tmp_ver "15.0") "docbar_2002.arx")
                    ((= tmp_ver "16.0") "docbar_2004.arx")
                    ((= tmp_ver "16.1") "docbar_2005.arx")
                    ((= tmp_ver "16.2") "docbar_2006.arx")
                    ((= tmp_ver "17.0") "docbar_2007.arx")
                    ((= tmp_ver "17.1") "docbar_2008.arx")
                    ((= tmp_ver "17.2") "docbar_2009.arx")
                    ((= tmp_ver "18.0") "docbar_2010.arx")
                    ((= tmp_ver "18.1") "docbar_2011.arx")
                    ((= tmp_ver "18.2") "docbar_2012.arx")
                    (T nil)
              )
)
(if (and tmp_str (not (member tmp_str (arx)))(findfile tmp_str))(vl-catch-all-apply 'arxload (list tmp_str)))


;;;-----------------------------------------
;;; 加载iDwgTab图形文档切换标签(如果有的话)
;;;-----------------------------------------
(cond 
    ((= (getenv "PROCESSOR_ARCHITECTURE") "x86")
        (setq tmp_str (cond ((= (substr tmp_ver 1 2) "15") "iDwgTab2000.arx")
                            ((= (substr tmp_ver 1 2) "16") "iDwgTab2004.arx")
                            ((= (substr tmp_ver 1 2) "17") "iDwgTab2007.arx")
                            ((= (substr tmp_ver 1 2) "18") "iDwgTab2010.arx")
                            ((= (substr tmp_ver 1 4) "19.0") "iDwgTab2013.arx")
                            (T nil)
                      )
        )
    )
    (t  (setq tmp_str (cond ((= tmp_ver "17.0") "iDwgTab2007x.arx")
                            ((= (substr tmp_ver 1 2) "17") "iDwgTab2008x.arx")
                            ((= (substr tmp_ver 1 2) "18") "iDwgTab2010x.arx")
                            ((= (substr tmp_ver 1 4) "19.0") "iDwgTab2013x.arx")
                            (T nil)
                      )
        )
    )
)
(if (and tmp_str (not (member tmp_str (arx)))(findfile tmp_str))(vl-catch-all-apply 'arxload (list tmp_str)))


;;;-------------------------------------------
;;; 加载cl-DwgMan图形文档切换标签(如果有的话)
;;;-------------------------------------------
(cond 
    ((= (getenv "PROCESSOR_ARCHITECTURE") "x86")
        (setq tmp_str (cond ((= (substr tmp_ver 1 2) "16") "cl-DwgMan_2006-x86.arx")
                            ((= tmp_ver "17.0") "cl-DwgMan_2007-x86.arx")
                            ((= tmp_ver "17.1") "cl-DwgMan_2008-x86.arx")
                            ((= tmp_ver "17.2") "cl-DwgMan_2009-x86.arx")
                            ((= tmp_ver "18.0") "cl-DwgMan_2010-x86.arx")
                            ((= tmp_ver "18.1") "cl-DwgMan_2011-x86.arx")
                            ((= tmp_ver "18.2") "cl-DwgMan_2012-x86.arx")
                            ((= tmp_ver "19.0") "cl-DwgMan_2013-x86.arx")
                            (T nil)
                      )
        )
    )
    (t  (setq tmp_str (cond ((= tmp_ver "17.1") "cl-DwgMan_2008-x64.arx")
                            ((= tmp_ver "17.2") "cl-DwgMan_2009-x64.arx")
                            ((= tmp_ver "18.0") "cl-DwgMan_2010-x64.arx")
                            ((= tmp_ver "18.1") "cl-DwgMan_2011-x64.arx")
                            ((= tmp_ver "18.2") "cl-DwgMan_2012-x64.arx")
                            ((= tmp_ver "19.0") "cl-DwgMan_2013-x64.arx")
                            (T nil)
                      )
        )
    )
)
(if (and tmp_str (not (member tmp_str (arx)))(findfile tmp_str))(vl-catch-all-apply 'arxload (list tmp_str)))


;;;------------------------------
;;; 加载乱刀去教育版(如果有的话)
;;;------------------------------
(setq tmp_str (substr (getvar "ACADVER") 1 2)
      tmp_str (strcat "BladeR" tmp_str (if (= (getenv "PROCESSOR_ARCHITECTURE") "x86") "" "_X64") ".arx")
)
(if (and (not (member tmp_str (arx)))(findfile tmp_str))(vl-catch-all-apply 'arxload (list tmp_str)))


;;;------------------------------
;;; 加载UnEdu去教育版(如果有的话)
;;;------------------------------
(setq tmp_str (substr (getvar "ACADVER") 1 2)
      tmp_str (strcat "UnEdu_R" tmp_str (if (= (getenv "PROCESSOR_ARCHITECTURE") "x86") "" "_X64") ".arx")
)
(if (and (not (member tmp_str (arx)))(findfile tmp_str))(vl-catch-all-apply 'arxload (list tmp_str)))
(setq tmp_str nil tmp_ver nil)



;;;----------------------------------------------------
;;;  打造一个适合自己的运行环境 (前置分号等于取消该行)
;;;----------------------------------------------------
;(setvar "MIRRTEXT" 0)             ; 镜像文字:否
;(setvar "SORTENTS" 127)          ; 图元排序顺序
;(setvar "CHAMFERA" 0)             ; 倒角的长度1
;(setvar "CHAMFERB" 0)             ; 倒角的长度2
;(setvar "AUNITS"   0)             ; 角度单位:十进制度数
;(setvar "AUPREC"   4)            ; 角度精度
;(setvar "LUNITS"   2)             ; 线性单位
;(setvar "LUPREC"   4)            ; 线性精度
;(setvar "FILEDIA"  1)             ; 显示文件定位对话框:是
;(setvar "CMDDIA"   1)             ; 显示外部命令及plot对话框:是
;(setvar "BLIPMODE" 0)            ; 控制点标记(小十字)是否可见:否
;(setvar "GRIDMODE" 0)             ; 显示点栅格:否
;(setvar "SNAPMODE" 0)             ; 捕捉间距:否
;(setvar "LISPINIT" 1)             ; AutoLISP 函数和变量只在当前绘图任务中有效
;(setvar "UCSICON"  1)            ; 使UCS光标不移动
;(setvar "CURSORSIZE" 100)        ; 十字光标最大化
;(setvar "PSLTSCALE" 0)           ; 1:视口比例决定线型比例
;(setvar "DIMZIN"   8)            ; 控制是否对主单位值作消零处理, 8:消除后续零
(if (> (getvar "SAVETIME") 60)    ; 以指定的时间间隔自动保存图形，
    (setvar "SAVETIME" 10)        ; 若大于1小时则改为10分钟，缺省保存在系统的临时文件夹
)

;(if (getvar "DIMASSOC") (setvar "DIMASSOC" 1))          ; 1:使用非关联标注,2:使用关联标注对象
;(if (getvar "QAFLAGS") (setvar "QAFLAGS" 0))
;(if (getvar "PICKSTYLE") (setvar "PICKSTYLE" 1))        ; 1:使用编组选择,3:使用编组选择和关联填充选择
;(if (getvar "DBLCLKEDIT") (setvar "DBLCLKEDIT" 1))      ; 控制绘图区域中的双击编辑操作
;(if (getvar "OSOPTIONS") (setvar "OSOPTIONS" 2))        ; 1:对象捕捉忽略图案填充对象
;(if (getvar "AUTOSNAP") (setvar "AUTOSNAP" 63))         ; 控制自动捕捉标记、工具栏提示和磁吸的显示等
;(if (getvar "MEASUREMENT") (setvar "MEASUREMENT" 1))    ; 控制当前图形是使用英制还是公制填充图案和线型文件 0:英制 1:公制
;(if (getvar "STARTMODE") (setvar "STARTMODE" 0))        ; 控制“开始”选项卡的显示 初始值1


;;----- 可选一些缺省设定 -----
;(setq YQ_NOWALLSEAL nil)                   ; ww 画墙命令无封口线
;(setq YQ_PIERIGNOREINTERS nil)             ; 开门窗命令时墙垛起点计算忽略所有交点
;(setq YQ_VAR_DOOR '(900 200 nil nil))      ; 设置缺省门宽、墙跺、是否居墙中、净宽以门套为准
;(setq YQ_VAR_WIN '(1500 200 T))            ; 设置缺省窗宽、墙跺、是否居墙中
;(setq YQ_VAR_AZHFACTOR 0.2)                ; 自动轴号因子:短于x倍轴线长度的轴线不画轴号
;(setq YQ_VAR_LTFACTOR 0.15)                ; 自动调节轴线线型比例系数=此数字*图纸比例/LTSCALE
;(setq YQ_VAR_HIDDEN '("HIDDEN" 15 0.5))    ; 虚线设定: 虚线名称,模型空间中的线型比例,图纸空间中的线型比例

;(setq YQ_NOLAYERSTACK nil)                 ; 关闭/锁定/隔离等图层命令不再使用还原用堆栈
;(setq YQ_NOAUTOFITEXCEL nil)               ; 实时输出Excel表格时不再自动调节单元格宽度
;(setq YQ_ISUPDATEDIMLAYER nil)             ; sd 缩放标注时，是否同时修改为当前标注图层
;(setq YQ_VAR_WINDOW '(45 30 25 50))        ; 参数窗常量: 落地门扇厚,重叠部分半宽,半玻璃厚,半看线墙厚
;(setq YQ_NOUPDATELAYER t)                  ; 转换图层快捷键ersc时，不更新原有图层

;(setq YQ_HASCMDSTAT t)                     ; 可开启cmdstats.txt文件统计源泉设计命令使用频率
;(setq YQ_WARNINGMINUTES 240)               ; 当前图纸连续工作若干分钟后给出温馨提示，nil/0: 不提示
;(setq YQ_NORECOMMENDVPSCALE t)             ; sd命令时，不自动“推荐当前视口比例”
;(setq YQ_ZXSIZE 2)                         ; 控制原始折断线符号的大小，缺省值:2

;(if (/= (getvar "INSUNITS") 4)
;(if (yq_warning "提示" (strcat "此图绘图单位是 <" (yq_unitsstr) ">，是否修正为毫米?"))
;    (yq_init_mm nil)    ; 用于图纸单位自动初始化为毫米, nil 可修改为预设比例，如100
;)
;)


;;----- 新的语句可放在这里 -----








(princ) ;为最后一行
