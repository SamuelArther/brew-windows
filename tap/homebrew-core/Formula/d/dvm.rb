class Dvm < Formula
  desc "Docker Version Manager"
  homepage "https://howtowhale.github.io/dvm/"
  url "https://github.com/howtowhale/dvm/archive/refs/tags/1.0.3.tar.gz"
  sha256 "148c2c48a17435ebcfff17476528522ec39c3f7a5be5866e723c245e0eb21098"
  license "Apache-2.0"
  head "https://github.com/howtowhale/dvm.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "ef6bf87ce1cfe30b17791df64526e0153177bc19145c6c47ccc0dc11800c180d"
  end

  depends_on "go" => :build

  def install
    system "make", "VERSION=#{version}", "UPGRADE_DISABLED=true"
    prefix.install "dvm.sh"
    bash_completion.install "bash_completion" => "dvm"
    (prefix/"dvm-helper").install "dvm-helper/dvm-helper"
  end

  def caveats
    <<~EOS
      dvm is a shell function, and must be sourced before it can be used.
      Add the following command to your bash profile:
          [ -f #{opt_prefix}/dvm.sh ] && . #{opt_prefix}/dvm.sh
    EOS
  end

  test do
    output = shell_output("bash -c 'source #{prefix}/dvm.sh && dvm --version'")
    assert_match "Docker Version Manager version #{version}", output
  end
end
