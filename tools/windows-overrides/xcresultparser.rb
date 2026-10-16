class Xcresultparser < Formula
  desc "Parse binary .xcresult bundles from Xcode builds and test runs"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "xcresultparser", "xcresultparser"
  end

  test do
    assert_match "2.2.0", shell_output("#{bin}/xcresultparser -v")
    # A minimal .xcresult bundle in xcresulttool's typed object encoding (objects may be stored uncompressed).
    key = ->(s) { "K#{s.bytesize}:#{s}" }
    obj = ->(type, body) { "[S#{type.bytesize}:#{type}#{body}]" }
    str = ->(s) { obj.call("String", key.call("_v") + "V#{s.bytesize}:#{s}") }
    field = ->(name, value) { key.call(name) + value }
    test = lambda do |name, status, duration|
      obj.call("ActionTestMetadata", field.call("duration", obj.call("Double", key.call("_v") + "V#{duration.bytesize}:#{duration}")) +
        field.call("identifier", str.call("DemoTests/#{name}")) + field.call("name", str.call(name)) +
        field.call("testStatus", str.call(status)))
    end
    group = ->(name, children) { obj.call("ActionTestSummaryGroup", field.call("name", str.call(name)) + field.call("subtests", obj.call("Array", children))) }
    tests = obj.call("ActionTestPlanRunSummaries", field.call("summaries", obj.call("Array",
      obj.call("ActionTestPlanRunSummary", field.call("name", str.call("Demo Plan")) + field.call("testableSummaries", obj.call("Array",
        obj.call("ActionTestableSummary", field.call("name", str.call("DemoTests")) + field.call("tests", obj.call("Array",
          group.call("All tests", group.call("DemoTests.xctest", group.call("DemoTests",
            test.call("testOne()", "Success", "0.25") + test.call("testTwo()", "Failure", "0.5")))))))))))))
    failure = obj.call("TestFailureIssueSummary", field.call("documentLocationInCreatingWorkspace", obj.call("DocumentLocation",
      field.call("url", str.call("file:///src/DemoTests.swift#StartingLineNumber=41")))) +
      field.call("issueType", str.call("Assertion Failure")) + field.call("message", str.call("XCTAssertEqual failed")) +
      field.call("testCaseName", str.call("DemoTests.testTwo()")))
    root = obj.call("ActionsInvocationRecord", field.call("actions", obj.call("Array", obj.call("ActionRecord",
      field.call("actionResult", obj.call("ActionResult", field.call("issues", obj.call("ResultIssueSummaries",
        field.call("testFailureSummaries", obj.call("Array", failure)))) +
        field.call("testsRef", obj.call("Reference", field.call("id", str.call("0~tests")))))) +
      field.call("endedTime", obj.call("Date", key.call("_v") + "V28:2024-05-01T10:00:02.500+0000")) +
      field.call("runDestination", obj.call("ActionRunDestinationRecord", field.call("displayName", str.call("My PC")))) +
      field.call("startedTime", obj.call("Date", key.call("_v") + "V28:2024-05-01T10:00:00.000+0000"))))) +
      field.call("metrics", obj.call("ResultMetrics", field.call("warningCount", obj.call("Int", key.call("_v") + "V1:2")))))
    (testpath/"Demo.xcresult/Data").mkpath
    (testpath/"Demo.xcresult/Data/data.0~root").write root
    (testpath/"Demo.xcresult/Data/data.0~tests").write tests
    (testpath/"Demo.xcresult/Info.plist").write <<~XML
      <?xml version="1.0" encoding="UTF-8"?>
      <plist version="1.0"><dict><key>rootId</key><dict><key>hash</key><string>0~root</string></dict></dict></plist>
    XML
    output = shell_output("#{bin}/xcresultparser -o txt #{testpath}/Demo.xcresult")
    assert_match "Number of tests = 2", output
    assert_match "Number of warnings = 2", output
    assert_match "DemoTests.swift:42: XCTAssertEqual failed", output
    junit = shell_output("#{bin}/xcresultparser -o junit #{testpath}/Demo.xcresult")
    assert_match '<testcase name="testTwo()" time="0.5" classname="DemoTests">', junit
    assert_match "Execution time = 2.5s", output
  end
end
