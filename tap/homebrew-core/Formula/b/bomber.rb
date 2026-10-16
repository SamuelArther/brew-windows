class Bomber < Formula
  desc "Scans Software Bill of Materials for security vulnerabilities"
  homepage "https://devops-kung-fu.github.io/bomber/"
  url "https://github.com/devops-kung-fu/bomber/archive/refs/tags/v0.5.1.tar.gz"
  sha256 "f4d8165ea9d3be0e88fdb33d35870588df308f31a4c40f14f09f0b68570f6ae1"
  license "MPL-2.0"
  head "https://github.com/devops-kung-fu/bomber.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "df5c41221d9c9735526483f6cea74dfc06dceea986042d44d850742ab8afc1a1"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args

    generate_completions_from_executable(bin/"bomber", shell_parameter_format: :cobra)

    pkgshare.install "_TESTDATA_"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/bomber --version")

    cp pkgshare/"_TESTDATA_/sbom/bomber.spdx.json", testpath
    output = shell_output("#{bin}/bomber scan bomber.spdx.json")
    assert_match "Total vulnerabilities found:", output
  end
end
