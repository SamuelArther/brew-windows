class Macvim < Formula
  desc "GUI for vim, using the official gVim build on Windows"
  homepage "https://www.vim.org/"
  url "https://github.com/vim/vim-win32-installer/releases/download/v9.2.1202/gvim_9.2.1202_x64.zip"
  sha256 "a8956f116755c4ad0995d50d2b04387d12e3bd1488306476fa61ba70c47e868e"
  license "Vim"

  # MacVim is the macOS GUI port of Vim; its Windows counterpart is gVim from the official
  # vim/vim-win32-installer build. mvim/gvim open the GUI, vim/vi/view/ex are the console Vim.

  livecheck do
    url :stable
    strategy :github_latest
  end

  conflicts_with "vim", because: "both install vi* binaries"

  def install
    install_tree
    %w[mvim gvim].each { |cmd| windows_exe "libexec/vim92/gvim.exe", cmd }
    %w[mvimdiff gvimdiff].each { |cmd| windows_exe "libexec/vim92/gvim.exe", cmd, "-d" }
    %w[mview gview].each { |cmd| windows_exe "libexec/vim92/gvim.exe", cmd, "-R" }
    %w[vim vi].each { |cmd| windows_exe "libexec/vim92/vim.exe", cmd }
    windows_exe "libexec/vim92/vim.exe", "view", "-R"
    windows_exe "libexec/vim92/vim.exe", "vimdiff", "-d"
    windows_exe "libexec/vim92/vim.exe", "ex", "-e"
    windows_exe "libexec/vim92/xxd.exe", "xxd"
  end

  test do
    assert_match "VIM - Vi IMproved", shell_output("#{bin}/vim --version")
    (testpath/"test.txt").write("Hello World from MacVim!!")
    system bin/"vim", "-es", "-i", "NONE", "-u", "NONE", "+s/MacVim/gVim/g", "+wq", "test.txt"
    assert_equal "Hello World from gVim!!", (testpath/"test.txt").read.chomp
  end
end
