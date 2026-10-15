class Emacs < Formula
  desc "GNU Emacs text editor"
  homepage "https://www.gnu.org/software/emacs/"
  version "31.1"
  url "https://ftpmirror.gnu.org/gnu/emacs/windows/emacs-31/emacs-31.1.zip"
  sha256 "96a1018e9de877e77768ca884007f1a9df44889e9ae81ae3ca3d904b5681b8b6"
  license "GPL-3.0-or-later"

  # Official GNU Emacs Windows x64 build (fetched through the GNU mirror redirector).

  def install
    install_tree
    windows_exe "libexec/bin/emacs.exe", "emacs"
    windows_exe "libexec/bin/emacsclient.exe", "emacsclient"
    windows_exe "libexec/bin/runemacs.exe", "runemacs"
    windows_exe "libexec/bin/etags.exe", "etags"
    windows_exe "libexec/bin/ebrowse.exe", "ebrowse"
  end

  test do
    assert_equal "4", shell_output("#{bin}/emacs --batch --eval=\"(print (+ 2 2))\"").strip
  end
end
