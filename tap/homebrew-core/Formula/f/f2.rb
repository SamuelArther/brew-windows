class F2 < Formula
  desc "Command-line batch renaming tool"
  homepage "https://f2.freshman.tech"
  url "https://github.com/ayoisaiah/f2/archive/refs/tags/v2.2.2.tar.gz"
  sha256 "0785e40b1fd2adb55165f668dc2635d47559fd7534b0f1da33849f155c4e539b"
  license "MIT"
  head "https://github.com/ayoisaiah/f2.git", branch: "master"

  # Upstream may add/remove tags before releasing a version, so we check
  # GitHub releases instead of the Git tags.
  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "88d1b7b591e9e39c919b76da2bcc0c747b34c60006e5a2c1092ca4c52b2cd45e"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./cmd/f2"

    bash_completion.install "scripts/completions/f2.bash" => "f2"
    fish_completion.install "scripts/completions/f2.fish"
    zsh_completion.install "scripts/completions/f2.zsh" => "_f2"
  end

  test do
    touch "test1-foo.foo"
    touch "test2-foo.foo"
    system bin/"f2", "-s", "-f", ".foo", "-r", ".bar", "-x"
    assert_path_exists testpath/"test1-foo.bar"
    assert_path_exists testpath/"test2-foo.bar"
    refute_path_exists testpath/"test1-foo.foo"
    refute_path_exists testpath/"test2-foo.foo"
  end
end
