;;;=========================================================================
;;;         源 泉 设 计    [组合命令面板]相关LSP程序例子
;;;-------------------------------------------------------------------------
;;; 文件夹 sys\
;;;=========================================================================
;;; 注：代码行中分号";"后面是注释，行首加分号则暂时取消该行。

;;; 代码编写: Walter Lam   QQ: 575825448
;;; 复制传播本文件请保留以上信息


;;;======================
;;; 一些自定义命令的例子
;;;======================

(defun c:yq_mi (/ ss p1 p2)
    (yq_cbegin)
    (princ "\n****镜像")
    (if (and (setq ss (ssget ":l"))
             (yq_ssredraw ss 3)
             (setq p1 (getpoint "\n指定镜像线的第一点:"))
             (setq p2 (getpoint p1 "\n指定镜像线的第二点:"))
        )
        (if (getpoint "\n点击鼠标删除原图元: <空格保留原图元> ")
            (yq_mirror ss p1 p2 t)
            (yq_mirror ss p1 p2 nil)
        )
    )
    (yq_cend)
)


(defun c:yq_ccb(/ pt)
    (princ "\n****带基点剪切")
    (and (setq pt (getpoint "\n基点: "))
         (ssget)
         (command "copybase" "non" pt "p" "" "erase" "p" "")
    )
    (princ)
)


(defun c:yq_rz(/ pt1 pt2)
    (princ "\n****旋转到0度")
    (and (ssget)
         (setq pt1 (getpoint "\n基点: "))
         (setq pt2 (getpoint pt1 "\n点取角度,此角度将改为0度: "))
         (command "rotate" "p" "" "non" pt1 "r" "non" pt1 "non" pt2 "0")
    )
    (princ)
)


(defun c:yq_rg()
    (princ "\n****局部再生")
    (if (ssget) (command "._erase" "p" "" "._oops"))
    (princ)
)


(defun c:yq_zd()
    (princ "\n****视图缩小0.5倍")
    (command "._zoom" "0.5x")
    (princ)
)


(defun c:yq_ze()
    (princ "\n****视图缩放到范围")
    (command "._zoom" "e")
    (princ)
)


(defun c:yq_zz()
    (princ "\n****缩放到上一个视图")
    (command "._zoom" "p")
    (princ)
)


(defun c:yq_edb(/ ss la)
    (princ "\n****炸开块到块所在层")
    (if (setq ss (ssget '((0 . "INSERT"))))
        (foreach e (yq_ss2lst ss)
            (yq_mark)
            (setq la (cdr (assoc 8 (entget e))))
            (command "._explode" e) (yq_cmdenter)
            (command "._chprop" (yq_newss) "" "la" la "")
        )
    )
    (princ)
)


(defun c:yq_edc(/ ss)
    (princ "\n****炸开块到当前层")
    (if (setq ss (ssget '((0 . "INSERT"))))
        (foreach e (yq_ss2lst ss)
            (yq_mark)
            (command "._explode" e) (yq_cmdenter)
            (command "._chprop" (yq_newss) "" "la" (getvar "CLAYER") "")
        )
    )
    (princ)
)


(defun c:yq_vxx(/ pt1 pt2 ss)
    (princ "\n****不拖拽移动")
    (and (setq ss (ssget))
         (yq_ssredraw ss 3)
         (setq pt1 (getpoint "\n基点: "))
         (setq pt2 (getpoint pt1 "\n目标点: "))
         (command "._move" ss "" "non" pt1 "non" pt2)
    )
    (if ss (yq_ssredraw ss 4))
    (princ)
)


(defun c:yq_cxx(/ pt1 pt2 ss)
    (princ "\n****不拖拽复制")
    (if (and (setq ss (ssget))
             (yq_ssredraw ss 3)
             (setq pt1 (getpoint "\n基点: "))
        )
        (while (setq pt2 (getpoint pt1 "\n目标点: "))
             (command "._copy" ss "" "non" pt1 "non" pt2)
        )
    )
    (if ss (yq_ssredraw ss 4))
    (princ)
)


(defun c:yq_date ( / pt str )
    (princ "\n****插入日期时间字串\n")
    (if (setq pt (getpoint))
        (progn
            (setq str (rtos (getvar "CDATE") 2 6)
                  str (strcat (substr str 1 4) "-" (substr str 5 2) "-" (substr str 7 2)
                              " " (substr str 10 2) ":" (substr str 12 2))
            )
            (yq_text pt (* 3 (getvar "DIMSCALE")) str)
        )
    )
    (princ)
)


(defun c:yq_cm(/ ss pt1 pt2 n d a ss0)
    (yq_cbegin)
    (princ "\n****按次数复制")
    (if (and (setq ss (ssget))
             (setq pt1 (getpoint "\n基点: "))
             (setq pt2 (getpoint pt1 "\n第二点: "))
        )
        (progn
            (setq ss0 (yq_copy2pt ss pt1 pt2))
            (initget 2)
            (setq yq_cm (if yq_cm yq_cm 1)
                  n (getint (strcat "\n复制次数(负数内等分): <" (itoa yq_cm) "> "))
                  yq_cm (if n n yq_cm)
                  d (distance pt1 pt2)
                  a (angle pt1 pt2)
            )
            (if (minusp yq_cm) 
                (setq d (/ d (abs yq_cm)))
                (setq pt1 pt2 ss ss0)
            )
            (setq pt2 (polar pt1 a d))
            (repeat (1- (abs yq_cm))
                (yq_copy2pt ss  pt1 pt2)
                (setq pt2 (polar pt2 a d))
            )
        )
    )
    (yq_cend)
)


;;;==============
;;; SOLID变HATCH
;;;==============
;;; 2012.7.17 写
(defun c:yq_s2h(/ ss e)
    (princ "\n****SOLID变HATCH")
    (if (setq ss (ssget '((0 . "SOLID,TRACE"))))
        (foreach x (yq_ss2lst ss)
            (setq e (yq_pline (yq_getvertexes x nil) 1))
            (command "_bhatch" "p" "s" "s" e "" "")
            (command "_matchprop" x (entlast) "")
            (entdel x)(entdel e)
        )
    )
    (princ)
)


;;;========
;;; 弧变圆
;;;========
;;; 2012.12.20 写
(defun c:yq_a2c(/ ss en)
    (princ "\n****弧变圆")
    (if (setq ss (ssget '((0 . "ARC"))))
        (foreach x (yq_ss2lst ss)
            (setq en (cddr (entget x))
                  en (vl-remove-if '(lambda (x) (member (car x) '(50 51))) en)
            )
            (entmake (cons '(0 . "CIRCLE") en))
            (entdel x)
        )
    )
    (princ)
)



;;;============
;;; 圆变多段线
;;;============
;;; 2012.6.30 写
(defun c:yq_c2x(/ ss)
    (princ "\n****圆变多段线")
    (if (setq ss (ssget '((0 . "CIRCLE"))))
        (foreach x (yq_ss2lst ss)
            (yq_cirle2donut x)
        )
    )
    (princ)
)



(defun c:yq_expattblk(/ ss)
    (yq_cbegin)
    (princ "\n****炸开属性块,文字不变")
    (if (setq ss (ssget (list (cons 0 "INSERT"))))
    (foreach e (yq_ss2lst ss)
        (yq_explodeattblk e)
    )
    )
    (yq_cend)
)



(defun c:yq_fxx(/ ss)
    (yq_cbegin)
    (princ "\n****批量圆角多段线")
    (setq yq_fxx (if yq_fxx yq_fxx 0))
    (setq yq_fxx (yq_getdist "\n圆角半径: " yq_fxx))
    (and (setq ss (ssget '((0 . "LWPOLYLINE"))))
         (foreach e (yq_ss2lst ss)
             (yq_filletpline e yq_fxx)
         )
    )
    (yq_cend)
)



(defun c:yq_cfxx(/ ss)
    (yq_cbegin)
    (princ "\n****批量倒角多段线")
    (setq yq_cfxx (if yq_cfxx yq_cfxx 0))
    (setq yq_cfxx (yq_getdist "\n倒角距离: " yq_cfxx))
    (and (setq ss (ssget '((0 . "LWPOLYLINE"))))
         (foreach e (yq_ss2lst ss)
             (yq_chamferpline e yq_cfxx)
         )
    )
    (yq_cend)
)



(defun c:yq_delendspaces(/ ss)
    (yq_cbegin)
    (princ "\n****去除文字前后空格")
    (and (setq ss (ssget (list (cons 0 "*TEXT"))))
         (foreach x (yq_ss2lst ss)
             (yq_updent x (list (cons 1 (yq_allTrim (yq_dxf x 1))))) 
         )
    )
    (yq_cend)
)



;;;==================
;;; 快速切割提取图像
;;;==================
(defun c:yq_qgtx(/ e1 e2 e3 e4)
    (yq_cbegin)
    (princ "\n****快速切割提取图像")
    (if (and (setq e1 (car (yq_entsel "\n选取要剪切的图像: " (list (cons 0 "IMAGE")))))
             (setq e2 (car (yq_entsel "\n选取多段线边界: " (list (cons 0 "*POLYLINE,CIRCLE,ELLIPSE")))))
             (setq e3 (yq_clone e1))
        )
    (prgon
        (setq e4 (yq_pline (yq_curve2ptlst_d e2) 1))
        (if (= (yq_dxf e3 280) 0)
            (command "_imageclip" e3 "n" "s" e4)
            (command "_imageclip" e3 "d" "_imageclip" e3 "n" "s" e4)
        )
        (entdel e4)
        (command "_move" (entlast) "" (car (yq_getvertexes e2 0)))
    )
    )
    (yq_cend)
)



;;;============
;;; 炸开参照块
;;;============
(defun c:yq_refx (/ e)
    (yq_cbegin)
    (princ "\n****炸开参照块")
    (if (setq e (car (yq_entsel "\n请选取参照块: " (list (cons 0 "INSERT")))))
    (progn
        (command "_xref" "b" (yq_dxf e 2))
        (command "_explode" e)
    )
    )
    (yq_cend)
)



;;;=================================
;;; 颜色/线性/线宽恢复为随层bylayer
;;;=================================
(defun c:yq_00(/ ss)
    (yq_cbegin)
    (princ "\n****颜色/线性/线宽恢复为随层bylayer")
    (cond 
        ((setq ss (yq_sspre))
            (command "._chprop" ss "" "lt" "bylayer" "lw" "bylayer" "c" "bylayer" "")
            (princ "\n所选图元颜色/线性/线宽恢复为随层bylayer.")
        )
        (t
           	(setvar "CECOLOR" "bylayer")
           	(setvar "CELTYPE" "bylayer")
           	(setvar "CELWEIGHT" -1)
           	(princ "\n当前颜色/线性/线宽恢复为随层bylayer.")
	       )
	   )
   	(yq_cend)
)


;;----- 新的语句可放在这里 -----







(princ)                           ; 为最后一行
