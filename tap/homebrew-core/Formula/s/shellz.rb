class Shellz < Formula
  desc "Small utility to track and control custom shellz"
  homepage "https://github.com/evilsocket/shellz"
  url "https://github.com/evilsocket/shellz/archive/refs/tags/v1.6.0.tar.gz"
  sha256 "3a89e3d573563a0c2ccb1831ff41fc0204c8b4efb011c10108ab98451a309b1c"
  license "GPL-3.0-only"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "15dc8f050bd0a5aad81e49e9b85c58597c221aa3ddae48382c1179543fea25b6"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./cmd/shellz"
  end

  test do
    output = shell_output("#{bin}/shellz -no-banner -no-effects -path #{testpath}", 1)
    assert_match "creating", output
    assert_path_exists testpath/"shells"
  end
end
