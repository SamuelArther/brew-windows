class K2tf < Formula
  desc "Kubernetes YAML to Terraform HCL converter"
  homepage "https://github.com/sl1pm4t/k2tf"
  url "https://github.com/sl1pm4t/k2tf/archive/refs/tags/v0.8.0.tar.gz"
  sha256 "9efdac448a99dbdda558eb93b63ed0b3ccabbac43c14df21ef3ba9bd402a4003"
  license "MPL-2.0"
  head "https://github.com/sl1pm4t/k2tf.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "232bcaf5b48a986c9e52c40f1e47ed51a158fefc065a7003ddc34065ad0bbd67"
  end

  depends_on "go" => :build

  resource "homebrew-test", :test do
    url "https://raw.githubusercontent.com/sl1pm4t/k2tf/b1ea03a68bd27b34216c080297924c8fa2a2ad36/test-fixtures/service.tf.golden"
    sha256 "c970a1f15d2e318a6254b4505610cf75a2c9887e1a7ba3d24489e9e03ea7fe90"
  end

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.version=#{version}")

    pkgshare.install "test-fixtures"
  end

  test do
    cp pkgshare/"test-fixtures/service.yaml", testpath
    testpath.install resource("homebrew-test")
    system bin/"k2tf", "-f", "service.yaml", "-o", testpath/"service.tf"
    assert compare_file(testpath/"service.tf.golden", testpath/"service.tf")

    assert_match version.to_s, shell_output("#{bin}/k2tf --version")
  end
end
