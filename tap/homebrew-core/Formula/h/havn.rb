class Havn < Formula
  desc "Fast configurable port scanner with reasonable defaults"
  homepage "https://github.com/mrjackwills/havn"
  url "https://github.com/mrjackwills/havn/releases/download/v0.3.10/havn_windows_x86_64.zip"
  sha256 "782c657d4ebe8461d7eeadc359205101dab058271ab1050df4e7c9c1013af2fe"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mrjackwills/havn/archive/refs/tags/v0.3.10.tar.gz"
  end

  def install
    bin.install "havn.exe"
  end

  test do
    output = shell_output("#{bin}/havn example.com -p 443 -r 6")
    assert_match "1 open\e[0m, \e[31m0 closed", output

    assert_match version.to_s, shell_output("#{bin}/havn --version")
  end
end
