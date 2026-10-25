class Hellwal < Formula
  desc "Fast, extensible color palette generator"
  homepage "https://github.com/danihek/hellwal"
  url "https://github.com/danihek/hellwal/archive/refs/tags/v1.0.9.tar.gz"
  sha256 "f708cbe7326299ac14a9a39869f5a833d58668ec7bbc679fea8b24915b307864"
  license "MIT"

  deny_network_access!

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "8c4167f83bc50256ee6a1a1289ce4afed1bbd5fd77db85f4b6d352d44ac7ba6c"
  end

  def install
    system "make", "install", "DESTDIR=#{bin}"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/hellwal --version")

    (testpath/"hw.theme").write "%% color0  = #282828 %%"
    output = shell_output("#{bin}/hellwal --skip-term-colors -j -t hw.theme 2>&1", 1)
    assert_match "Not enough colors were specified in color palette", output
  end
end
