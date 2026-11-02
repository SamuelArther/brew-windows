class ActionsBatch < Formula
  desc "Time-sharing supercomputer built on GitHub Actions"
  homepage "https://blog.alexellis.io/github-actions-timesharing-supercomputer/"
  url "https://github.com/alexellis/actions-batch/archive/refs/tags/v0.0.3.tar.gz"
  sha256 "9290b338e41ff71fb599de9996c64e33a58ec9aa4e8fdd7c4484ec2b085f2160"
  license "MIT"
  head "https://github.com/alexellis/actions-batch.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "f734b3ae318dbd9d96a603475fdc46fe240a8a931f19c680e228dce0cd0bfbba"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args

    pkgshare.install "examples"
  end

  test do
    # fake token file
    (testpath/"notvavlid").write "fake"

    cmd = "#{bin}/actions-batch --private=false --owner alexellis " \
          "--token-file #{testpath}/notvavlid --runs-on ubuntu-latest " \
          "--org=false --file #{pkgshare}/examples/curl.sh"

    output = shell_output("#{cmd} 2>&1", 2)
    assert_match "failed to create repo", output
  end
end
