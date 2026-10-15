class Tealdeer < Formula
  desc "Very fast implementation of tldr in Rust"
  homepage "https://tealdeer-rs.github.io/tealdeer/"
  url "https://github.com/tealdeer-rs/tealdeer/releases/download/v1.9.0/tealdeer-windows-x86_64-msvc.exe"
  sha256 "23fb76fb683d76dd413cb470b1a3c32157e585edec7d740e73da1aa9c6406dfe"
  license any_of: ["Apache-2.0", "MIT"]

  livecheck do
    url :stable
    strategy :github_latest
  end

  conflicts_with "tlrc", because: "both install `tldr` binaries"

  def install
    bin.install "tealdeer-windows-x86_64-msvc.exe" => "tldr.exe"
  end

  test do
    assert_match "tealdeer #{version}", shell_output("#{bin}/tldr --version")
  end
end
