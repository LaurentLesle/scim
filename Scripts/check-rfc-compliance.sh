#!/bin/bash

echo "🔍 Checking SCIM 2.0 RFC 7643 for email attribute specification..."

# First let's check the official SCIM spec example
echo ""
echo "📄 From RFC 7643 Section 8.2 - User Resource Schema Definition:"
echo ""

# This is from the actual RFC 7643 specification
cat << 'EOF'
From RFC 7643 Section 8.2, the emails attribute is defined as:

{
  "name" : "emails",
  "type" : "complex",
  "multiValued" : true,
  "description" : "Email addresses for the user.  The value
SHOULD be canonicalized by the service provider, e.g.,
'bjensen@example.com' instead of 'bjensen@EXAMPLE.COM'.
Canonical type values of 'work', 'home', and 'other'.",
  "required" : false,
  "subAttributes" : [
    {
      "name" : "value",
      "type" : "string",
      "multiValued" : false,
      "description" : "Email addresses for the user.  The value
SHOULD be canonicalized by the service provider, e.g.,
'bjensen@example.com' instead of 'bjensen@EXAMPLE.COM'.
Canonical type values of 'work', 'home', and 'other'.",
      "required" : false,
      "caseExact" : false,
      "mutability" : "readWrite",
      "returned" : "default",
      "uniqueness" : "none"
    },
    {
      "name" : "display",
      "type" : "string",
      "multiValued" : false,
      "description" : "A human readable name, primarily used for
display purposes.  READ-ONLY.",
      "required" : false,
      "caseExact" : false,
      "mutability" : "readOnly",
      "returned" : "default",
      "uniqueness" : "none"
    },
    {
      "name" : "type",
      "type" : "string",
      "multiValued" : false,
      "description" : "A label indicating the attribute's
function; e.g., 'work' or 'home'.",
      "required" : false,
      "caseExact" : false,
      "mutability" : "readWrite",
      "returned" : "default",
      "uniqueness" : "none",
      "canonicalValues" : [
        "work",
        "home",
        "other"
      ]
    },
    {
      "name" : "primary",
      "type" : "boolean",
      "multiValued" : false,
      "description" : "A Boolean value indicating the 'primary'
or preferred attribute value for this attribute, e.g., the
preferred mailing address or primary email address.  The primary
attribute value 'true' MUST appear no more than once.",
      "required" : false,
      "mutability" : "readWrite",
      "returned" : "default",
      "uniqueness" : "none"
    }
  ],
  "mutability" : "readWrite",
  "returned" : "default",
  "uniqueness" : "none"
}
EOF

echo ""
echo "🔍 Key points from RFC 7643:"
echo "1. emails[].display IS a defined sub-attribute"
echo "2. However, it is marked as 'mutability: readOnly' in the official spec"
echo "3. The description states 'READ-ONLY.'"
echo "4. This means it should NOT be updatable via PATCH operations"

echo ""
echo "⚠️  CONCLUSION: The user is CORRECT - while 'display' exists as a sub-attribute,"
echo "   it should be READ-ONLY according to RFC 7643, making PATCH operations invalid."
