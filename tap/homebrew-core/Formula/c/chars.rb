class Chars < Formula
  desc "Command-line tool to display information about unicode characters"
  homepage "https://github.com/boinkor-net/chars/"
  url "https://github.com/boinkor-net/chars/archive/refs/tags/v0.7.0.tar.gz"
  sha256 "2f79843a3b1173870b41ebce491a54812b13a44090d0ae30a6f572caa91f0736"
  license "MIT"
  head "https://github.com/boinkor-net/chars.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "63d3ef833c2ac63292904cbebf5ab303006dd7d652a35282aa830c9ed0c148d6"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    cd "chars" do
      system "cargo", "install", *std_cargo_args
    end
  end

  test do
    output = shell_output "#{bin}/chars 1C"
    assert_match "Control character", output
    assert_match "FS", output
    assert_match "File Separator", output
  end
end
