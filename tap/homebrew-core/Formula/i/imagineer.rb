class Imagineer < Formula
  desc "Image processing and conversion from the terminal"
  homepage "https://github.com/foresterre/imagineer"
  url "https://github.com/foresterre/imagineer/archive/refs/tags/v0.25.1.tar.gz"
  sha256 "16ed8ebe47f7af676b333a8abc11bf60b0ab490bbda0de9cf8a04cbf50d5f8c9"
  license any_of: ["Apache-2.0", "MIT"]
  head "https://github.com/foresterre/imagineer.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "14416ceb4e92e2ef1834ba65441110725bbfa3fca10f882ac6fcf957cd4a47c8"
  end

  depends_on "rust" => :build

  on_intel do
    depends_on "nasm" => :build
  end

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ig --version")

    system bin/"ig", "--input", test_fixtures("test.png"), "--output", "out.jpg"
    assert_path_exists testpath/"out.jpg"
  end
end
