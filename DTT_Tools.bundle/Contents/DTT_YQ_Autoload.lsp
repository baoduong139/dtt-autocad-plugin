;;; =========================================================================
;;; DTOOLS PRO - YQARCH & MENU AUTOLOADER (SAFE LAZY LOADING MODE)
;;; KHÔNG tự động nạp Menu hoặc chạy command khi AutoCAD khởi động
;;; Chỉ thực thi khi người dùng gõ lệnh DTT_LOAD_YQ hoặc YQ_RELOAD
;;; =========================================================================

(vl-load-com)

(defun c:DTT_LOAD_YQ (/ curDir cuixPath lspPath yqSys pref sp err)
  (princ "\n⚡ [DTools Pro]: Đang nạp hệ thống YQArch...")
  
  ;; 1. Luôn bật thanh MenuBar
  (setvar "MENUBAR" 1)

  ;; 2. Xác định thư mục bundle hiện hành một cách tương đối
  (setq curDir (vl-filename-directory (findfile "DTT_YQ_Autoload.lsp")))
  (if (null curDir)
      (setq curDir (vl-filename-directory (findfile "DTT_AutoCAD_Plugin.dll")))
  )

  ;; 3. Xác định thư mục YQArch sys bên trong Bundle (tương đối 100%, không dùng đường dẫn máy dev)
  (setq yqSys (if curDir (strcat curDir "\\yqarch\\sys") nil))
  (if (or (null yqSys) (not (vl-file-directory-p yqSys)))
      (setq yqSys (if curDir (strcat curDir "\\yqarch\\yqarch\\sys") nil))
  )

  ;; 4. Thêm đường dẫn sys của YQArch vào AutoCAD Support File Search Path
  (if (and yqSys (vl-file-directory-p yqSys))
    (vl-catch-all-apply
      '(lambda ()
         (setq pref (vla-get-files (vla-get-preferences (vlax-get-acad-object))))
         (setq sp (vla-get-supportpath pref))
         (if (not (vl-string-search "yqarch" (strcase sp)))
           (vla-put-supportpath pref (strcat sp ";" yqSys))
         )
       )
    )
  )

  ;; 5. Tìm file menu cuix bên trong bundle
  (setq cuixPath (if curDir (strcat curDir "\\Menu\\yqarch.cuix") nil))
  (if (or (null cuixPath) (not (findfile cuixPath)))
      (setq cuixPath (if yqSys (strcat yqSys "\\yqarch.cuix") nil))
  )

  ;; 6. Khử trùng các nhóm menu YQArch rác / phiên bản khác
  (setvar "FILEDIA" 0)
  (foreach mg '("YQARCH_CN" "YQARCH_ENG" "GYQARCH" "YQARCH_VN")
    (if (menugroup mg)
      (vl-catch-all-apply 'vla-unload (list (vla-item (vla-get-menugroups (vlax-get-acad-object)) mg)))
    )
  )

  ;; 7. Nạp Menu CUIX an toàn
  (if (and (null (menugroup "yqarch")) cuixPath (findfile cuixPath))
      (vl-catch-all-apply
        '(lambda ()
           (command "_.menuload" cuixPath)
           (princ "\n[DTools Pro] Đã nạp Menu YQArch vào AutoCAD!")
         )
      )
  )
  (setvar "FILEDIA" 1)

  ;; 8. Nạp động cơ YQArch
  (if yqSys
    (vl-catch-all-apply
      '(lambda ()
         (setq lspPath (strcat yqSys "\\yqstart.lsp"))
         (if (findfile lspPath) (load lspPath))
         (if (findfile (strcat yqSys "\\yqarch.vlx")) (load (strcat yqSys "\\yqarch.vlx")))
         (princ "\n[DTools Pro] Đã nạp động cơ YQArch vào AutoCAD!")
       )
    )
  )
  (princ "\n⚡ [DTools Pro]: Hoàn tất nạp YQArch.\n")
  (princ)
)

(defun c:YQ_RELOAD () (c:DTT_LOAD_YQ))
(defun c:DTT_AUTO_INIT_YQ () (c:DTT_LOAD_YQ))

;; Ghi chú: KHÔNG tự động gọi c:DTT_LOAD_YQ tại đây để tuân thủ nguyên tắc Safe Startup (Lazy Loading)
(princ "\n[DTools Pro] YQArch module ready. Type 'DTT_LOAD_YQ' to load menu.\n")
(princ)
