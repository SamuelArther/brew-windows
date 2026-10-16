class Dufs < Formula
  desc "Static file server"
  homepage "https://github.com/sigoden/dufs"
  url "https://github.com/sigoden/dufs/releases/download/v0.46.0/dufs-v0.46.0-x86_64-pc-windows-msvc.zip"
  sha256 "21d829653ac1f178b124ed384e2a57df19b99ae404935cba9a1a5719c199b0bf"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/sigoden/dufs/archive/refs/tags/v0.46.0.tar.gz"
  end

  def install
    bin.install "dufs.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dufs --version 2>&1")
  end
end
