class Precious < Formula
  desc "One code quality tool to rule them all"
  homepage "https://github.com/houseabsolute/precious"
  url "https://github.com/houseabsolute/precious/releases/download/v1.0.0/precious-Windows-msvc-x86_64.zip"
  sha256 "f3f11d4b7c8825b2e834f6cec4c24405163031dbf3000bc1e1596d844d1151d5"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/houseabsolute/precious/archive/refs/tags/v1.0.0.tar.gz"
  end

  def install
    bin.install "precious.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/precious --version")

    (testpath/"test.rs").write "fn main() {}\n"
    system bin/"precious", "config", "init", "--auto"
    assert_path_exists testpath/"precious.toml"
    assert_match "rustfmt", (testpath/"precious.toml").read
  end
end
