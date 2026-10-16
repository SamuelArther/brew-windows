class Ktmpl < Formula
  desc "Parameterized templates for Kubernetes manifests"
  homepage "https://github.com/jimmycuadra/ktmpl"
  url "https://github.com/jimmycuadra/ktmpl/archive/refs/tags/0.9.1.tar.gz"
  sha256 "3377f10477775dd40e78f9b3d65c3db29ecd0553e9ce8a5bdcb8d09414c782e9"
  license "MIT"
  head "https://github.com/jimmycuadra/ktmpl.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "e43458a201b1ed92a1fb0e7f7fd3c1a38d1d4632b5e28d8675d0a52b2103204e"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    (testpath/"test.yml").write <<~YAML
      ---
      kind: "Template"
      apiVersion: "v1"
      metadata:
        name: "test"
      objects:
        - kind: "Service"
          apiVersion: "v1"
          metadata:
            name: "test"
          spec:
            ports:
              - name: "test"
                protocol: "TCP"
                targetPort: "$((PORT))"
            selector:
              app: "test"
      parameters:
        - name: "PORT"
          description: "The port the service should run on"
          required: true
          parameterType: "int"
    YAML
    system bin/"ktmpl", "test.yml", "-p", "PORT", "8080"
  end
end
