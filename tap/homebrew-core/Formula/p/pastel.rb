class Pastel < Formula
  desc "Command-line tool to generate, analyze, convert and manipulate colors"
  homepage "https://github.com/sharkdp/pastel"
  url "https://github.com/sharkdp/pastel/releases/download/v0.12.0/pastel-v0.12.0-x86_64-pc-windows-gnu.zip"
  sha256 "11897f155324a3a474ee825a6375e746969c6f89799efb01bf71c023abf9acde"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/sharkdp/pastel/archive/refs/tags/v0.12.0.tar.gz"
  end

  def install
    bin.install "pastel.exe"
  end

  test do
    output = shell_output("#{bin}/pastel format hex rebeccapurple").strip

    assert_equal "#663399", output
  end
end
