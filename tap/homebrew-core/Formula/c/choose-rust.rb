class ChooseRust < Formula
  desc "Human-friendly and fast alternative to cut and (sometimes) awk"
  homepage "https://github.com/theryangeary/choose"
  url "https://github.com/theryangeary/choose/releases/download/v1.3.7/choose-x86_64-pc-windows-gnu.exe"
  sha256 "9233beade020c3e74854911a15aa973f7dc9f4253abe8856b8c273579a555903"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/theryangeary/choose/archive/refs/tags/v1.3.7.tar.gz"
  end

  def install
    bin.install "choose-x86_64-pc-windows-gnu.exe" => "choose.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/choose --version 2>&1")
  end
end
