class Goawk < Formula
  desc "POSIX-compliant AWK interpreter written in Go"
  homepage "https://benhoyt.com/writings/goawk/"
  url "https://github.com/benhoyt/goawk/releases/download/v1.32.0/goawk_v1.32.0_windows_amd64.zip"
  sha256 "2cebd8d64a8eaf84b2787edec8c0274760166e077bc2bdbb69d41537b56cf32c"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/benhoyt/goawk/archive/refs/tags/v1.32.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/goawk.exe", "goawk"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/goawk --version 2>&1")
  end
end
