class Graphqurl < Formula
  desc "Curl for GraphQL with autocomplete, subscriptions and GraphiQL"
  homepage "https://github.com/hasura/graphqurl"
  url "https://registry.npmjs.org/graphqurl/-/graphqurl-2.0.0.tgz"
  sha256 "589fd91ec8b40554ff2d32a35846bc9e31466ce9824530ccd3176aafe8e8ce75"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "875d3aa2b3320b52b648dace7ebaf06fc0e053d97f9bad894f458d750f2fc6f8"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")

    # Build an `:all` bottle by removing log file.
    node_modules = libexec/"lib/node_modules/graphqurl/node_modules"
    rm node_modules/"@oclif/linewrap/yarn-error.log"
  end

  test do
    output = Utils.safe_popen_read(bin/"gq", "https://graphqlzero.almansi.me/api",
                                              "--header", "Content-Type: application/json",
                                              "--introspect")
    assert_match "type Query {", output
  end
end
