class Kepubify < Formula
  desc "Convert ebooks from epub to kepub"
  homepage "https://pgaskin.net/kepubify/"
  url "https://github.com/pgaskin/kepubify/releases/download/v4.0.4/kepubify-windows-64bit.exe"
  sha256 "f3dee3ece063329661fef474262fe403812335bf1676eab35b672565bf195738"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/pgaskin/kepubify/archive/refs/tags/v4.0.4.tar.gz"
  end

  def install
    bin.install "kepubify-windows-64bit.exe" => "covergen.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/covergen --version 2>&1")
  end
end
