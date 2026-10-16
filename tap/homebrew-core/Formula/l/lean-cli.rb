class LeanCli < Formula
  desc "Command-line tool to develop and manage LeanCloud apps"
  homepage "https://github.com/leancloud/lean-cli"
  url "https://github.com/leancloud/lean-cli/archive/refs/tags/v1.2.4.tar.gz"
  sha256 "1d7c355b3060a35a8679e446cbe504423e09b55296c044a51a27a23c9298329b"
  license "Apache-2.0"
  head "https://github.com/leancloud/lean-cli.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "544da6758eccac8930d8d240677c777577708a39b19d3c82557dc5ddb55da643"
  end

  depends_on "go" => :build

  conflicts_with "elan-init", because: "both install `lean` binaries"

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    build_from = build.head? ? "homebrew-head" : "homebrew"
    system "go", "build", *std_go_args(output: bin/"lean", ldflags: "-X main.pkgType=#{build_from}"), "./lean"

    bin.install_symlink "lean" => "tds"

    bash_completion.install "misc/lean-bash-completion" => "lean"
    zsh_completion.install "misc/lean-zsh-completion" => "_lean"
  end

  test do
    assert_match "lean version #{version}", shell_output("#{bin}/lean --version")
    output = shell_output("#{bin}/lean login --region us-w1 --token foobar 2>&1", 1)
    assert_match "[ERROR] User doesn't sign in.", output
  end
end
