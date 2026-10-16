class Komac < Formula
  desc "Community Manifest Creator for Windows Package Manager (WinGet)"
  homepage "https://github.com/russellbanks/Komac"
  url "https://github.com/russellbanks/Komac/releases/download/v2.17.1/komac-2.17.1-x86_64-pc-windows-msvc.exe"
  sha256 "a97c002c5ffa15f232af117efac6774e755d27acc47eba00f1661033267d7438"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/russellbanks/Komac/archive/refs/tags/v2.17.1.tar.gz"
  end

  def install
    bin.install "komac-2.17.1-x86_64-pc-windows-msvc.exe" => "komac.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/komac --version 2>&1")
  end
end
