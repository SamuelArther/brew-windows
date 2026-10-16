class Rizin < Formula
  desc "UNIX-like reverse engineering framework and command-line toolset"
  homepage "https://rizin.re"
  url "https://github.com/rizinorg/rizin/releases/download/v0.9.1/rizin-windows-shared64-v0.9.1.zip"
  sha256 "45ffa004e26653eaee8d262b8e6eeae09402a7d319c1f20a2ba794bd6af1cc28"
  license "LGPL-3.0-only"
  revision 2

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/rizinorg/rizin/releases/download/v0.9.1/rizin-src-v0.9.1.tar.xz"
  end

  def install
    install_tree
    windows_exe "libexec/bin/rizin.exe", "rizin"
    windows_exe "libexec/bin/rz-ar.exe", "rz-ar"
    windows_exe "libexec/bin/rz-asm.exe", "rz-asm"
    windows_exe "libexec/bin/rz-ax.exe", "rz-ax"
    windows_exe "libexec/bin/rz-bin.exe", "rz-bin"
    windows_exe "libexec/bin/rz-diff.exe", "rz-diff"
    windows_exe "libexec/bin/rz-find.exe", "rz-find"
    windows_exe "libexec/bin/rz-gg.exe", "rz-gg"
    windows_exe "libexec/bin/rz-hash.exe", "rz-hash"
    windows_exe "libexec/bin/rz-run.exe", "rz-run"
    windows_exe "libexec/bin/rz-sign.exe", "rz-sign"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/rizin -version 2>&1")
  end
end
