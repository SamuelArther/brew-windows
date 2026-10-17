class Cig < Formula
  desc "CLI app for checking the state of your git repositories"
  homepage "https://github.com/stevenjack/cig"
  url "https://github.com/stevenjack/cig/archive/refs/tags/v0.1.5.tar.gz"
  sha256 "545a4a8894e73c4152e0dcf5515239709537e0192629dc56257fe7cfc995da24"
  license "MIT"
  head "https://github.com/stevenjack/cig.git", branch: "master"

  # Last release on 2015-07-19. Needs patches from closed PR to build on maintained Go
  deprecate! date: "2026-07-10", because: :unmaintained
  disable! date: "2027-07-10", because: :unmaintained

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "de3322f8d46e21cb6403fecde55afcb2b0e411d314c47c7f83e470aea411df7e"
  end

  depends_on "go" => :build

  # Patch to remove godep dependency.
  patch do
    url "https://github.com/stevenjack/cig/compare/2d834ee..f0e78f0.patch?full_index"
    sha256 "3aa14ecfa057ec6aba08d6be3ea0015d9df550b4ede1c3d4eb76bdc441a59a47"
    type :unofficial
    resolves "https://github.com/stevenjack/cig/pull/44"
  end

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    repo_path = "#{testpath}/test"
    system "git", "init", "--bare", repo_path
    (testpath/".cig.yaml").write <<~YAML
      test_project: #{repo_path}
    YAML
    system bin/"cig", "--cp=#{testpath}"
  end
end
