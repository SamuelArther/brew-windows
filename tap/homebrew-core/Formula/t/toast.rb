class Toast < Formula
  desc "Tool for running tasks in containers"
  homepage "https://github.com/stepchowfun/toast"
  url "https://github.com/stepchowfun/toast/releases/download/v0.48.0/toast-x86_64-pc-windows-msvc.exe"
  sha256 "19352cf093cb6569cdfecc4a58f123fc71684307723eb5b2ba56f8c9143d27c8"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/stepchowfun/toast/archive/refs/tags/v0.48.0.tar.gz"
  end

  def install
    bin.install "toast-x86_64-pc-windows-msvc.exe" => "toast.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/toast --version 2>&1")
  end
end
