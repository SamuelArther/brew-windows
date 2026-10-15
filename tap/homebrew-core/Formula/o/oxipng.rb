class Oxipng < Formula
  desc "Multithreaded PNG optimizer written in Rust"
  homepage "https://github.com/oxipng/oxipng"
  version "10.2.1"
  url "https://github.com/oxipng/oxipng/releases/download/v10.2.1/oxipng-10.2.1-x86_64-pc-windows-msvc.zip"
  sha256 "7e940f83ee46874b73f53031f96a15834cb70b220af27391fb06fe7b4dd798e1"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "oxipng.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/oxipng --version")
  end
end
