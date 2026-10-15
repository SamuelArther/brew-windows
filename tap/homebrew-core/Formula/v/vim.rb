class Vim < Formula
  desc "Vi 'workalike' with many additional features"
  homepage "https://www.vim.org/"
  version "9.2.1192"
  url "https://github.com/vim/vim-win32-installer/releases/download/v9.2.1192/gvim_9.2.1192_x64.zip"
  sha256 "7afd73e7b453edba211a80f7c45208ef645f4d13554986de67a2d843aa1fc6b1"
  license "Vim"

  # Official Vim Windows build (vim/vim-win32-installer). Windows builds are not published for
  # every patch, so this is the newest one rather than Homebrew's exact 9.2.1150.

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/vim92/vim.exe", "vim"
    windows_exe "libexec/vim92/gvim.exe", "gvim"
    windows_exe "libexec/vim92/xxd.exe", "xxd"
  end

  test do
    (testpath/"test.txt").write("Hello World from Vim!!")
    system bin/"vim", "-es", "-i", "NONE", "-u", "NONE", "+s/Vim/Vi/g", "+wq", "test.txt"
    assert_equal "Hello World from Vi!!", (testpath/"test.txt").read.chomp
  end
end
