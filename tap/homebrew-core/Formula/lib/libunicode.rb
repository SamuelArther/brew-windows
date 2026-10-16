class Libunicode < Formula
  desc "Modern C++20 Unicode library"
  homepage "https://github.com/contour-terminal/libunicode"
  url "https://github.com/contour-terminal/libunicode/releases/download/v0.9.4/unicode-query-windows-x86_64.exe"
  sha256 "9f74c70dd1f01da6b1c2f17a47b3927d4799f939844b5b2f33d8b453c2f5a30e"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/contour-terminal/libunicode/archive/refs/tags/v0.9.4.tar.gz"
  end

  def install
    bin.install "unicode-query-windows-x86_64.exe" => "unicode-query.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/unicode-query --version 2>&1")
  end
end
