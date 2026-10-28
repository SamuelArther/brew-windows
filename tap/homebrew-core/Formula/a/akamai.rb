class Akamai < Formula
  desc "CLI toolkit for working with Akamai's APIs"
  homepage "https://github.com/akamai/cli"
  url "https://github.com/akamai/cli/archive/refs/tags/v2.0.6.tar.gz"
  sha256 "fb614f97f816a7517f94d46a4a3eca74459f12db22245a0a96e3325aa1f3308a"
  license "Apache-2.0"
  head "https://github.com/akamai/cli.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "0c412e1bc8dfa4e7eee6e1bc1e1bb74d5eee6d650861040cb7d7f4ca018ec816"
  end

  depends_on "go" => [:build, :test]

  # `test do` block installs a CLI package from GitHub
  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    tags = %w[
      noautoupgrade
      nofirstrun
    ]
    system "go", "build", *std_go_args(tags:), "./cli"
  end

  test do
    assert_match "diagnostics", shell_output("#{bin}/akamai install diagnostics")
    system bin/"akamai", "uninstall", "diagnostics"
  end
end
