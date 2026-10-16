class TreallaProlog < Formula
  desc "Compact and efficient ISO Prolog interpreter written in plain old C"
  homepage "https://github.com/trealla-prolog/trealla-prolog"
  url "https://github.com/trealla-prolog/trealla-prolog/releases/download/v3.13.12/tpl-windows-x64.zip"
  sha256 "1e753315a6b78687a02bbdea1c370d1f08ae3e3dee42b1f0f2ca91db384823dc"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    throttle 5
  end

  def install
    install_tree
    windows_exe "libexec/tpl.exe", "tpl"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tpl --version 2>&1")
  end
end
