class Autorest < Formula
  desc "Swagger (OpenAPI) Specification code generator"
  homepage "https://github.com/Azure/autorest"
  url "https://registry.npmjs.org/autorest/-/autorest-3.8.0.tgz"
  sha256 "0d1739956d385d7f0951d4622078c73d9dada27340ca39cfaaa8ba7f8ab5bae1"
  license "MIT"

  deprecate! date: "2026-07-17", because: :deprecated_upstream
  disable! date: "2027-01-17", because: :deprecated_upstream

  bottle do
    sha256 cellar: :any, x86_64_windows: "f01cf576d3f338efb10a99ba34180e97595f70560d81dd8a64a92e9032df736e"
  end

  depends_on "node"

  resource "homebrew-petstore", :test do
    url "https://raw.githubusercontent.com/Azure/autorest/5c170a02c009d032e10aa9f5ab7841e637b3d53b/Samples/1b-code-generation-multilang/petstore.yaml"
    sha256 "e981f21115bc9deba47c74e5c533d92a94cf5dbe880c4304357650083283ce13"
  end

  allow_network_access! :test

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    resource("homebrew-petstore").stage do
      system (bin/"autorest"), "--input-file=petstore.yaml",
                               "--typescript",
                               "--output-folder=petstore"
      assert_includes File.read("petstore/package.json"), "Microsoft Corporation"
    end
  end
end
