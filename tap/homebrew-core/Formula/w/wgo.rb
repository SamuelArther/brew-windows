class Wgo < Formula
  desc "Watch arbitrary files and respond with arbitrary commands"
  homepage "https://github.com/bokwoon95/wgo"
  url "https://github.com/bokwoon95/wgo/releases/download/v0.7.2/wgo-windows.exe"
  sha256 "c13bc991e4572d7fb60b533331a99e8a878e8389e260d2c67e0b0f02d2a65d41"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/bokwoon95/wgo/archive/refs/tags/v0.7.2.tar.gz"
  end

  def install
    bin.install "wgo-windows.exe" => "wgo.exe"
  end

  test do
    output = shell_output("#{bin}/wgo -exit echo testing")
    assert_match "testing", output
  end
end
