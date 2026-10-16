class Akamai < Formula
  desc "CLI toolkit for working with Akamai's APIs"
  homepage "https://github.com/akamai/cli"
  url "https://github.com/akamai/cli/archive/refs/tags/v2.0.5.tar.gz"
  sha256 "42d47d0fdf4c737cf0cdfd882877f6e91bf416bb165b03acbce5c3d82aada299"
  license "Apache-2.0"
  head "https://github.com/akamai/cli.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "d05be26ef9f8fb1fcdb29935d68f7fbd90fd1d03cbb3d6d699a4e07fd4942f5e"
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
