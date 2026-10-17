class Graphqlviz < Formula
  desc "GraphQL Server schema visualizer"
  homepage "https://github.com/sheerun/graphqlviz"
  url "https://registry.npmjs.org/graphqlviz/-/graphqlviz-4.0.1.tgz"
  sha256 "1ede0553fe61ca6f59876b31a7d86f8f9aa692456255c1acf91c204feb2e1ef3"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "005282225b4b10bc81bdb9ead2e7b14e05e11e348606aab7c3804782f41dfeb4"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    test_file = testpath/"test.graphql"
    test_file.write <<~GRAPHQL
      type Query {
        hello: String
      }
    GRAPHQL

    output = pipe_output(bin/"graphqlviz", test_file.read)
    assert_match "digraph erd", output
    assert_match version.to_s, shell_output("#{bin}/graphqlviz --version")
  end
end
