;;; =========================================================================
;;; Dtools Pro - YQArch Initialization LISP Wrapper
;;; =========================================================================
(vl-load-com)

(setq *yq-dtools-dir* (vl-filename-directory (findfile "yqarch.lsp")))
(if (null *yq-dtools-dir*)
  (setq *yq-dtools-dir* (vl-filename-directory (findfile "DTT_AutoCAD_Plugin.dll")))
)

(if *yq-dtools-dir*
  (progn
    (setq *yq-sys-dir* (strcat *yq-dtools-dir* "\\sys"))
    (if (not (vl-file-directory-p *yq-sys-dir*))
      (setq *yq-sys-dir* (strcat *yq-dtools-dir* "\\yqarch\\sys"))
    )
    (if (vl-file-directory-p *yq-sys-dir*)
      (progn
        ;; 1. Thêm vào Support Search Path
        (setq pref (vla-get-files (vla-get-preferences (vlax-get-acad-object))))
        (setq sp (vla-get-supportpath pref))
        (if (not (vl-string-search "yqarch" (strcase sp)))
          (vla-put-supportpath pref (strcat sp ";" *yq-sys-dir*))
        )
        ;; 2. Nạp yqstart.lsp & yqarch.vlx
        (if (findfile (strcat *yq-sys-dir* "\\yqstart.lsp"))
          (load (strcat *yq-sys-dir* "\\yqstart.lsp"))
        )
        (if (and (null c:yq_about) (findfile (strcat *yq-sys-dir* "\\yqarch.vlx")))
          (load (strcat *yq-sys-dir* "\\yqarch.vlx"))
        )
        ;; 3. Nạp gói ngôn ngữ tiếng Việt
        (if (findfile (strcat *yq-dtools-dir* "\\YQArch Menu VietNam.VLX"))
          (load (strcat *yq-dtools-dir* "\\YQArch Menu VietNam.VLX"))
        )
      )
    )
  )
)
(princ)
