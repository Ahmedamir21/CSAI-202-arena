"""Builds docs/er/er_diagram.drawio from one description of the ER model.

Kept as a script (instead of one hand-written XML blob) so that a change in the model is a
one-line edit and the diagram can be regenerated instead of redrawn.

Usage:  python3 tools/build_er_drawio.py
"""

import html
import os

OUT = os.path.join("docs", "er", "er_diagram.drawio")

# --------------------------------------------------------------------------------------
# entities:  id, label, x, y, kind  (kind: strong | weak)
# --------------------------------------------------------------------------------------
E = [
    ("UA", "UserAccount", 80, 60, "strong"),
    ("CU", "Customer", 400, 60, "strong"),
    ("EM", "Employee", 720, 60, "strong"),
    ("CO", "Courier", 430, 260, "strong"),
    ("BE", "BranchEmployee", 800, 260, "strong"),
    ("BR", "Branch", 80, 430, "strong"),
    ("CI", "City", 400, 430, "strong"),
    ("ZO", "Zone", 720, 430, "strong"),
    ("AD", "Address", 1040, 430, "strong"),
    ("SH", "Shipment", 720, 640, "strong"),
    ("SS", "ShipmentStatusHistory", 1060, 640, "weak"),
    ("PT", "PackageType", 1400, 640, "strong"),
    ("SL", "ServiceLevel", 1680, 640, "strong"),
    ("PR", "PricingRule", 80, 860, "strong"),
    ("PA", "Payment", 400, 860, "strong"),
    ("CM", "Complaint", 720, 860, "strong"),
]
EW, EH = 180, 52

# --------------------------------------------------------------------------------------
# relationships:  id, label, (entityA, labelA), (entityB, labelB), totalA, totalB, offset
#   labelA/labelB are the (min,max) pairs written next to each entity
#   totalA/totalB  -> True means total participation (drawn as a double line)
#   offset moves the diamond along the perpendicular of the A->B line
# --------------------------------------------------------------------------------------
R = [
    ("R1", "HAS_CUSTOMER_PROFILE", "UA", "(0,1)", "CU", "(1,1)", False, True, 0),
    ("R2", "HAS_EMPLOYEE_RECORD", "UA", "(0,1)", "EM", "(1,1)", False, True, -70),
    ("R4", "SUPERVISES", "EM", "supervisor (0,n)", "EM", "subordinate (0,1)", False, False, 0),
    ("R5", "EMPLOYS", "BR", "(0,n)", "EM", "(1,1)", False, True, 60),
    ("R6", "MANAGED_BY", "BR", "(1,1)", "EM", "(0,1)", True, False, -60),
    ("R7", "BRANCH_IN_CITY", "BR", "(1,1)", "CI", "(0,n)", True, False, 0),
    ("R8", "CITY_IN_ZONE", "CI", "(1,1)", "ZO", "(0,n)", True, False, 0),
    ("R9", "OWNS_ADDRESS", "CU", "(0,n)", "AD", "(1,1)", False, True, 0),
    ("R10", "ADDRESS_IN_CITY", "AD", "(1,1)", "CI", "(0,n)", True, False, 0),
    ("R11", "SENDS", "CU", "(0,n)", "SH", "(1,1)", False, True, 0),
    ("R12", "PICKED_UP_FROM", "SH", "(1,1)", "AD", "(0,n)", True, False, 30),
    ("R13", "DELIVERED_TO", "SH", "(1,1)", "AD", "(0,n)", True, False, -30),
    ("R14", "PACKED_AS", "SH", "(1,1)", "PT", "(0,n)", True, False, 0),
    ("R15", "SHIPPED_WITH", "SH", "(1,1)", "SL", "(0,n)", True, False, 0),
    ("R16", "PRICED_BY", "SH", "(1,1)", "PR", "(0,n)", True, False, 0),
    ("R17", "HAS_STATUS_STEP", "SH", "(1,n)", "SS", "(1,1)", True, True, 0),
    ("R18", "RECORDED_BY", "SS", "(0,n)", "EM", "(0,1)", False, False, 0),
    ("R19", "LODGED_AT", "SS", "(0,n)", "BR", "(0,1)", False, False, 0),
    ("R20", "HANDLES", "CO", "(0,n)", "SH", "(0,n)", False, False, 0),
    ("R21", "TRANSFERS", "SH", "(0,n)", "BR", "(0,n)", False, False, 0),
    ("R22", "PAID_BY", "SH", "(0,n)", "PA", "(1,1)", False, True, 0),
    ("R23", "COLLECTED_BY", "PA", "(0,n)", "CO", "(0,1)", False, False, 0),
    ("R24", "FILES", "CU", "(0,n)", "CM", "(1,1)", False, True, 0),
    ("R25", "ABOUT", "CM", "(1,1)", "SH", "(0,n)", True, False, 0),
    ("R26", "RESOLVES", "EM", "(0,n)", "CM", "(0,1)", False, False, 0),
]
RW, RH = 190, 46

# attributes drawn on the diamonds of the two relationships that carry attributes
REL_ATTRS = {
    "R20": ["AssignmentType", "AssignedAt", "CompletedAt", "Notes"],
    "R21": ["SentAt", "ReceivedAt", "Notes"],
}

# --------------------------------------------------------------------------------------
# attributes of every entity, drawn as ellipses on pages 2 and 3
#   (key, dashed, double, composite) : key = primary key / partial key
# --------------------------------------------------------------------------------------
ATTRS = {
    "UserAccount": [
        ("UserID", "key"), ("Username", "unique"), ("PasswordHash", ""), ("PasswordSalt", ""),
        ("Role {Customer, Courier,<br/>BranchEmployee, Admin}", ""), ("IsActive", ""),
        ("CreatedAt", ""), ("LastLoginAt", ""), ("MustChangePassword", ""),
    ],
    "Customer": [
        ("CustomerID", "key"), ("FullName", ""), ("Email", "unique"),
        ("{Phone (Number, PhoneType)}", "multi"), ("CustomerType {Individual, Business}", ""),
        ("CompanyName", ""), ("CreatedAt", ""),
    ],
    "Employee": [
        ("EmployeeID", "key"), ("FullName", ""), ("Phone", ""), ("Email", ""), ("HireDate", ""),
        ("Salary", ""), ("AccessLevel (admins only)", ""), ("IsActive", ""),
    ],
    "Courier": [
        ("CourierID", "key"), ("LicenseNumber", "unique"),
        ("CourierStatus {Available, OnRoute, Off}", ""),
        ("Vehicle (PlateNumber, VehicleType,<br/>CapacityKg)", "composite"),
    ],
    "BranchEmployee": [("EmployeeID", "key"), ("Position {Clerk, Supervisor, Manager}", "")],
    "Branch": [
        ("BranchID", "key"), ("BranchName", ""), ("Street", ""), ("BuildingNo", ""),
        ("Phone", ""), ("IsActive", ""), ("ManagerStartDate", ""),
    ],
    "City": [("CityID", "key"), ("CityName", ""), ("GovernorateName", "")],
    "Zone": [("ZoneID", "key"), ("ZoneName", ""), ("Description", "")],
    "Address": [
        ("AddressID", "key"), ("Label {Home, Work, Warehouse, Other}", ""),
        ("AddressParts (Street, BuildingNo,<br/>ApartmentNo, FloorNo, Landmark)", "composite"),
        ("Notes", ""), ("IsDefault", ""),
    ],
    "Shipment": [
        ("ShipmentID", "key"), ("TrackingNumber", "unique"), ("ReceiverName", ""),
        ("ReceiverPhone", ""), ("WeightKg", ""), ("DeclaredValue", ""), ("IsInsured", ""),
        ("PaymentMethod", ""), ("CreatedAt", ""), ("ExpectedDeliveryDate", ""),
        ("DeliveredAt", ""), ("RatingStars", ""), ("RatingComment", ""), ("RatedAt", ""),
        ("CancelReason", ""), ("ProofReceiverName", ""), ("ProofCode", ""),
        ("Price", "derived"), ("CurrentStatus", "derived"),
    ],
    "ShipmentStatusHistory": [
        ("SeqNo", "partial"),
        ("StatusName {Created, PickedUp, AtBranch,<br/>InTransit, OutForDelivery, Delivered,<br/>FailedAttempt, Returned, Cancelled}", ""),
        ("ChangedAt", ""), ("Notes", ""),
    ],
    "PackageType": [("PackageTypeID", "key"), ("TypeName", "unique"), ("MaxWeightKg", ""), ("SizeNote", "")],
    "ServiceLevel": [("ServiceLevelID", "key"), ("LevelName", "unique"), ("PriceMultiplier", ""), ("MaxDays", "")],
    "PricingRule": [
        ("RuleID", "key"), ("BasePrice", ""), ("PricePerKg", ""), ("InsuranceRate", ""),
        ("EffectiveFrom", ""), ("EffectiveTo", ""), ("IsActive", ""),
    ],
    "Payment": [
        ("PaymentID", "key"), ("Amount", ""), ("PaymentMethod", ""),
        ("PaymentStatus {Pending, Paid, Refunded}", ""), ("PaidAt", ""), ("ReferenceNumber", ""),
    ],
    "Complaint": [
        ("ComplaintID", "key"), ("Subject", ""), ("Description", ""),
        ("ComplaintStatus {Open, InProgress, Resolved}", ""), ("OpenedAt", ""),
        ("Resolution", ""), ("ResolvedAt", ""),
    ],
}

PAGE2 = ["UserAccount", "Customer", "Employee", "Courier", "BranchEmployee", "Branch", "City", "Zone"]
PAGE3 = ["Address", "Shipment", "ShipmentStatusHistory", "PackageType", "ServiceLevel",
         "PricingRule", "Payment", "Complaint"]


def esc(s):
    return html.escape(s, quote=True)


def centre(eid):
    for i, lab, x, y, kind in E:
        if i == eid:
            return x + EW / 2, y + EH / 2
    raise KeyError(eid)


def build_page1():
    cells = []
    for eid, lab, x, y, kind in E:
        if kind == "weak":
            # double rectangle: an outer frame with no fill around an inner box
            cells.append(
                f'<mxCell id="{eid}_o" value="" style="rounded=0;whiteSpace=wrap;html=1;'
                f'fillColor=none;strokeColor=#000000;" vertex="1" parent="1">\n'
                f'  <mxGeometry x="{x}" y="{y}" width="{EW}" height="{EH}" as="geometry" />\n</mxCell>'
            )
            cells.append(
                f'<mxCell id="{eid}" value="{esc(lab)}" style="rounded=0;whiteSpace=wrap;html=1;'
                f'fillColor=#FFFFFF;strokeColor=#000000;fontStyle=1" vertex="1" parent="1">\n'
                f'  <mxGeometry x="{x + 7}" y="{y + 7}" width="{EW - 14}" height="{EH - 14}" as="geometry" />\n</mxCell>'
            )
        else:
            cells.append(
                f'<mxCell id="{eid}" value="{esc(lab)}" style="rounded=0;whiteSpace=wrap;html=1;'
                f'fillColor=#FFFFFF;strokeColor=#000000;fontStyle=1" vertex="1" parent="1">\n'
                f'  <mxGeometry x="{x}" y="{y}" width="{EW}" height="{EH}" as="geometry" />\n</mxCell>'
            )

    # inheritance triangle: apex up towards Employee, base towards the two subclasses
    cells.append(
        '<mxCell id="ISA" value="IS_A" style="shape=triangle;direction=north;whiteSpace=wrap;html=1;'
        'fillColor=#FFFFFF;strokeColor=#000000;fontSize=10;fontStyle=2" vertex="1" parent="1">\n'
        '  <mxGeometry x="520" y="130" width="330" height="95" as="geometry" />\n</mxCell>'
    )
    cells.append(
        '<mxCell id="e_isa" style="edgeStyle=orthogonalEdgeStyle;html=1;endArrow=none;" edge="1" '
        'parent="1" source="EM" target="ISA">\n  <mxGeometry relative="1" as="geometry" />\n</mxCell>'
    )
    cells.append(
        '<mxCell id="e_isa_c" value="" style="edgeStyle=orthogonalEdgeStyle;html=1;endArrow=none;" '
        'edge="1" parent="1" source="ISA" target="CO">\n  <mxGeometry relative="1" as="geometry" />\n</mxCell>'
    )
    cells.append(
        '<mxCell id="e_isa_b" value="" style="edgeStyle=orthogonalEdgeStyle;html=1;endArrow=none;" '
        'edge="1" parent="1" source="ISA" target="BE">\n  <mxGeometry relative="1" as="geometry" />\n</mxCell>'
    )
    # the note that says an admin is a role, not a sub-entity
    cells.append(
        '<mxCell id="adminnote" value="Admin is a ROLE of Employee (Role in UserAccount + '
        'AccessLevel in Employee), not a sub-entity." style="shape=note;whiteSpace=wrap;html=1;'
        'size=14;fillColor=#FFF2CC;strokeColor=#D6B656;fontSize=10;align=left;verticalAlign=middle;" '
        'vertex="1" parent="1">\n  <mxGeometry x="1010" y="140" width="180" height="90" as="geometry" />\n</mxCell>'
    )
    cells.append(
        '<mxCell id="e_adminnote" style="edgeStyle=orthogonalEdgeStyle;html=1;endArrow=none;dashed=1;" '
        'edge="1" parent="1" source="adminnote" target="EM">\n  <mxGeometry relative="1" as="geometry" />\n</mxCell>'
    )

    for rid, lab, a, la, b, lb, ta, tb, off in R:
        ax, ay = centre(a)
        bx, by = centre(b)
        if a == b:
            # recursive: draw the diamond above the entity
            dx, dy = ax + 60, ay - 150
        else:
            mx, my = (ax + bx) / 2, (ay + by) / 2
            vx, vy = bx - ax, by - ay
            n = (vx * vx + vy * vy) ** 0.5
            px, py = (-vy / n) * off, (vx / n) * off
            dx, dy = mx + px, my + py
        dx -= RW / 2
        dy -= RH / 2

        weak = rid == "R17"
        style = ("rhombus;whiteSpace=wrap;html=1;fillColor=#FFFFFF;strokeColor=#000000;"
                 "fontSize=10;fontStyle=2")
        if weak:
            cells.append(
                f'<mxCell id="{rid}_o" value="" style="{style}" vertex="1" parent="1">\n'
                f'  <mxGeometry x="{int(dx)}" y="{int(dy)}" width="{RW}" height="{RH}" as="geometry" />\n</mxCell>'
            )
            cells.append(
                f'<mxCell id="{rid}" value="{esc(lab)}" style="{style}" vertex="1" parent="1">\n'
                f'  <mxGeometry x="{int(dx) + 7}" y="{int(dy) + 7}" width="{RW - 14}" height="{RH - 14}" as="geometry" />\n</mxCell>'
            )
        else:
            cells.append(
                f'<mxCell id="{rid}" value="{esc(lab)}" style="{style}" vertex="1" parent="1">\n'
                f'  <mxGeometry x="{int(dx)}" y="{int(dy)}" width="{RW}" height="{RH}" as="geometry" />\n</mxCell>'
            )

        # the two edges; total participation is drawn as two parallel lines
        for side, target, label, total in (("a", a, la, ta), ("b", b, lb, tb)):
            eid = f"e_{rid}_{side}"
            base = ("edgeStyle=orthogonalEdgeStyle;html=1;endArrow=none;fontSize=10;"
                    "labelBackgroundColor=#FFFFFF;")
            if side == "a":
                src, dst = target, rid
            else:
                src, dst = rid, target
            if total:
                for k, offs in enumerate(("-3", "3")):
                    cells.append(
                        f'<mxCell id="{eid}_{k}" value="{esc(label) if k == 0 else ""}" style="{base}'
                        f'exitDy={offs};entryDy={offs};" edge="1" parent="1" source="{src}" target="{dst}">\n'
                        f'  <mxGeometry relative="1" as="geometry" />\n</mxCell>'
                    )
            else:
                cells.append(
                    f'<mxCell id="{eid}" value="{esc(label)}" style="{base}" edge="1" parent="1" '
                    f'source="{src}" target="{dst}">\n  <mxGeometry relative="1" as="geometry" />\n</mxCell>'
                )

        # attributes of the relationship, drawn as small ellipses under the diamond
        if rid in REL_ATTRS:
            for i, an in enumerate(REL_ATTRS[rid]):
                cells.append(
                    f'<mxCell id="{rid}_a{i}" value="{esc(an)}" style="ellipse;whiteSpace=wrap;html=1;'
                    f'fillColor=#FFFFFF;strokeColor=#000000;fontSize=9" vertex="1" parent="1">\n'
                    f'  <mxGeometry x="{int(dx) + 8}" y="{int(dy) + RH + 8 + i * 34}" width="150" '
                    f'height="30" as="geometry" />\n</mxCell>'
                )
                cells.append(
                    f'<mxCell id="{rid}_ae{i}" style="html=1;endArrow=none;" edge="1" parent="1" '
                    f'source="{rid}_a{i}" target="{rid}">\n  <mxGeometry relative="1" as="geometry" />\n</mxCell>'
                )

    # legend
    legend = [
        ("legend", "Legend: rectangle = entity, double rectangle = weak entity, "
                   "diamond = relationship, double diamond = identifying relationship, "
                   "triangle = inheritance, double line = total participation, "
                   "(min,max) = structural constraint."),
    ]
    cells.append(
        f'<mxCell id="{legend[0][0]}" value="{esc(legend[0][1])}" style="rounded=0;whiteSpace=wrap;html=1;'
        f'fillColor=#F5F5F5;strokeColor=#666666;fontSize=10;align=left;verticalAlign=top;spacingLeft=6;" '
        f'vertex="1" parent="1">\n  <mxGeometry x="1420" y="60" width="360" height="110" as="geometry" />\n</mxCell>'
    )
    return cells


def build_attr_page(names):
    cells = []
    x, y = 40, 40
    colw = 470
    maxrows = 0
    for idx, name in enumerate(names):
        col = idx % 3
        row = idx // 3
        bx = x + col * colw
        by = y + row * 0
        cells.append(
            f'<mxCell id="h_{name}" value="{esc(name)}" style="rounded=0;whiteSpace=wrap;html=1;'
            f'fillColor=#FFFFFF;strokeColor=#000000;fontStyle=1;fontSize=12" vertex="1" parent="1">\n'
            f'  <mxGeometry x="{bx}" y="{by}" width="200" height="{EH}" as="geometry" />\n</mxCell>'
        )
        att = ATTRS[name]
        for i, (an, kind) in enumerate(att):
            ay = by + EH + 26 + i * 44
            if an.startswith("(") and False:
                pass
            style = "ellipse;whiteSpace=wrap;html=1;fillColor=#FFFFFF;strokeColor=#000000;fontSize=9"
            label = an
            if kind == "key":
                style += ";fontStyle=4"      # underline
            elif kind == "partial":
                style += ";fontStyle=2;dashed=1"
                label = "<i>" + an + "</i>"
            elif kind == "multi":
                style += ";shape=doubleEllipse"
            elif kind == "derived":
                style += ";dashed=1"
            elif kind == "composite":
                style += ";fillColor=#F5F5F5"
            elif kind == "unique":
                style += ";fontStyle=2"
            cells.append(
                f'<mxCell id="a_{name}_{i}" value="{esc(label)}" style="{style}" vertex="1" parent="1">\n'
                f'  <mxGeometry x="{bx + 40}" y="{ay}" width="330" height="36" as="geometry" />\n</mxCell>'
            )
            cells.append(
                f'<mxCell id="ae_{name}_{i}" style="html=1;endArrow=none;" edge="1" parent="1" '
                f'source="h_{name}" target="a_{name}_{i}">\n  <mxGeometry relative="1" as="geometry" />\n</mxCell>'
            )
        maxrows = max(maxrows, len(att))
        # move the next column down is not needed: columns are independent
    return cells


def main():
    p1 = build_page1()
    p2 = build_attr_page(PAGE2)
    p3 = build_attr_page(PAGE3)

    parts = ['<mxfile host="app.diagrams.net" modified="2026-01-01T00:00:00.000Z" agent="SwiftShip" '
             'version="21.0.0" type="device">']
    for name, cells in (("1 - Entities and relationships", p1),
                        ("2 - Attributes (part 1)", p2),
                        ("3 - Attributes (part 2)", p3)):
        parts.append(f'  <diagram id="{"".join(c for c in name if c.isalnum())}" name="{name}">')
        parts.append('    <mxGraphModel dx="1400" dy="900" grid="0" gridSize="10" guides="1" '
                     'tooltips="1" connect="1" arrows="1" fold="1" page="1" pageScale="1" '
                     'pageWidth="2200" pageHeight="1700" math="0" shadow="0">')
        parts.append("      <root>")
        parts.append('        <mxCell id="0" />')
        parts.append('        <mxCell id="1" parent="0" />')
        for c in cells:
            parts.append("        " + c)
        parts.append("      </root>")
        parts.append("    </mxGraphModel>")
        parts.append("  </diagram>")
    parts.append("</mxfile>")

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", encoding="utf-8") as f:
        f.write("\n".join(parts) + "\n")
    print("wrote", OUT, os.path.getsize(OUT), "bytes")


if __name__ == "__main__":
    main()
