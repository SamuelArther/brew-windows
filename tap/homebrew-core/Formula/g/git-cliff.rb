class GitCliff < Formula
  desc "Highly customizable changelog generator"
  homepage "https://git-cliff.org/"
  url "https://github.com/orhun/git-cliff/releases/download/v2.14.2/git-cliff-2.14.2-x86_64-pc-windows-msvc.zip"
  sha256 "6ece2112b3f4af462190ecbea4aeb1315fff6af415629b597f84319073c31131"
  license all_of: ["Apache-2.0", "MIT"]

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "git-cliff.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/git-cliff --version")
  end
end
