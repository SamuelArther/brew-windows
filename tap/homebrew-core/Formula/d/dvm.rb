class Dvm < Formula
  desc "Docker Version Manager"
  homepage "https://howtowhale.github.io/dvm/"
  url "https://github.com/howtowhale/dvm/archive/refs/tags/1.0.3.tar.gz"
  sha256 "148c2c48a17435ebcfff17476528522ec39c3f7a5be5866e723c245e0eb21098"
  license "Apache-2.0"
  head "https://github.com/howtowhale/dvm.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "b7bb143bee6a586ee3670a43465d739028407dec9edb8b45169b9c33d71ca012"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

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
