class Fastmod < Formula
  desc "Fast, partial replacement for codemod (find/replace tool for programmers)"
  homepage "https://github.com/facebookincubator/fastmod"
  url "https://github.com/facebookincubator/fastmod/archive/refs/tags/v0.4.4.tar.gz"
  sha256 "b438cc7564ef34d01f27cdd3cd50ee66a9915b9c50939ca021c6bee2e9c1f069"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "674b558b1d829fbb15407ef20ddd7c9ca4fbb3e09b411f86d0d566f6f12157d0"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    (testpath/"input.txt").write("Hello, World!")
    system bin/"fastmod", "-d", testpath, "--accept-all", "World", "fastmod"
    assert_equal "Hello, fastmod!", (testpath/"input.txt").read
  end
end
