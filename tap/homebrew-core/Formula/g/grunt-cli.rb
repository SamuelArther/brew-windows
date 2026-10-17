class GruntCli < Formula
  desc "JavaScript Task Runner"
  homepage "https://gruntjs.com/"
  url "https://registry.npmjs.org/grunt-cli/-/grunt-cli-1.5.0.tgz"
  sha256 "4f7f52cf9f3bc62ebc7ae60d2db5c7f896cb0915ad1202dab9285d6117d7536d"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "647aa56fa25116efa68f6aee99b20087ef95d8c305b701567386f9a13ba0d19c"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    (testpath/"package.json").write <<~JSON
      {
        "name": "grunt-homebrew-test",
        "version": "1.0.0",
        "devDependencies": {
          "grunt": ">=0.4.0"
        }
      }
    JSON

    (testpath/"Gruntfile.js").write <<~JAVASCRIPT
      module.exports = function(grunt) {
        grunt.registerTask("default", "Write output to file.", function() {
          grunt.file.write("output.txt", "Success!");
        })
      };
    JAVASCRIPT

    system "npm", "install", *std_npm_args(prefix: false)
    system bin/"grunt"
    assert_path_exists testpath/"output.txt", "output.txt was not generated"
  end
end
